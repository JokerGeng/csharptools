#include "ip.h"
#include "icmp.h"
#include "tool.h"
#include <stdio.h>
#include <string.h>

extern const xipaddr_t netif_ipaddr;

/*
@param header ip头部
*/
static uint16_t checksum(uint16_t *header, uint16_t header_len)
{
    uint32_t sum = 0;
    for (int i = 0; i < header_len; i++)
    {
        // 逐个16位相加
        sum += header[i];
        if (sum & 0xffff0000)
        { // 如果产生了进位（高16位非0），则加回低16位
            sum = (sum & 0xffff) + (sum >> 16);
        }
    }
    // 对sum取反，并只保留低16位
    return ~sum & 0xffff;
}

void xip_in(xnet_packet_t *packet)
{
    xip_hdr_t *iphdr = (xip_hdr_t *)packet->data;
    if (iphdr->version != XNET_VERSION_IPV4)
    {
        fprintf(stderr, "Not IPV4!");
        return;
    }

    uint16_t head_size, total_len;

    head_size = iphdr->hdr_len * 4;
    total_len = swap_order16(iphdr->total_len);

    if (head_size < sizeof(xip_hdr_t) || total_len < head_size)
    {
        fprintf(stderr, "size failed!");
        return;
    }

    if (!xipaddr_is_equal_buf(&netif_ipaddr, iphdr->dest_ip))
    {
        fprintf(stderr, "not local!");
        return;
    }

    uint16_t rec_checksum = iphdr->hdr_checksum;
    iphdr->hdr_checksum = 0;
    uint16_t cal_checksum = checksum16((uint16_t *)iphdr, head_size, 0, 1);
    if (cal_checksum != rec_checksum)
    {
        fprintf(stderr, "checksum failed!");
        // printf("checksum failed!");
        return;
    }
    iphdr->hdr_checksum = rec_checksum;

    xipaddr_t src_ip;
    xipaddr_t dest_ip;
    memcpy(src_ip.array, iphdr->src_ip, XNET_IPV4_ADDR_SIZE);
    memcpy(dest_ip.array, iphdr->dest_ip, XNET_IPV4_ADDR_SIZE);

    switch (iphdr->protocol)
    {
    case XNET_PROTOCOL_UDP:
        /* code */
        break;
    case XNET_PROTOCOL_ICMP:
        remove_header(packet, sizeof(xip_hdr_t));
        icmp_in(&src_ip, packet);
        break;
    case XNET_PROTOCOL_TCP:
        /* code */
        break;

    default:
        break;
    }
}

static uint8_t *get_local_ip()
{
    return NULL;
}

void xip_out(xnet_protocol_t protocol, xipaddr_t *dst_ip, xnet_packet_t *package)
{
    static uint32_t ip_packet_id = 0;
    // 1.添加ip头
    xip_hdr_t *iphdr;
    add_header(package, sizeof(xip_hdr_t));

    iphdr = (xip_hdr_t *)package->data;
    iphdr->version = XNET_VERSION_IPV4;
    iphdr->hdr_len = sizeof(xip_hdr_t) / 4;
    iphdr->tos = 0;
    iphdr->total_len = package->size;
    iphdr->id = ip_packet_id;
    iphdr->flags_fragment = 0;
    iphdr->ttl = XNET_IP_DEFAULT_TTL;
    iphdr->protocol = protocol;
    iphdr->hdr_checksum = 0;
    memcpy(iphdr->src_ip, get_local_ip(), XNET_IPV4_ADDR_SIZE);
    memcpy(iphdr->dest_ip, dst_ip->array, XNET_IPV4_ADDR_SIZE);
    ip_packet_id++;
    ethernet_out(&dst_ip->addr, package);
}

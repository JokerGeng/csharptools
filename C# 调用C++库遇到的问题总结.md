## 接口导出方式
C++部分导出的接口要可以被C#或者Python等语言调用，不能像C++程序一样，可以直接调用，而是需要以C风格的形式导出接口，因为C++的重载特性，函数编译后实际的接口会发生变化，而不是源代码里面的接口。下面是导出接口示例：
```cpp
#ifdef __cplusplus
extern "C" {
#endif

#if defined(WIN32)  /* windows bindings */
    /** Building the C-API DLL **/
    #ifdef _OCSD_C_API_DLL_EXPORT
        #ifdef __cplusplus
            #define OCSD_C_API extern "C" __declspec(dllexport)
        #else
            #define OCSD_C_API __declspec(dllexport)
        #endif
    #else   
        /** building or using the static C-API library **/
        #if defined(_LIB) || defined(OCSD_USE_STATIC_C_API)
            #ifdef __cplusplus
                #define OCSD_C_API extern "C"
            #else
                #define OCSD_C_API
            #endif
        #else
        /** using the C-API DLL **/
            #ifdef __cplusplus
                #define OCSD_C_API extern "C" __declspec(dllimport)
            #else
                #define OCSD_C_API __declspec(dllimport)
            #endif
        #endif
    #endif
#else           /* linux bindings */
    #ifdef __cplusplus
        #define OCSD_C_API extern "C"
    #else
        #define OCSD_C_API
    #endif
#endif

/************************** type defination here *********************/


/************************** function declaration here *********************/


#ifdef __cplusplus
extern "C" {
#endif
```
> 说明：接口类型定义方法引用自opencsd库的C接口头文件。

## C# Marshal转换示例
### 结构体内的字符指针
说明：

1. 返回结构体内部含有字符指针成员的时候，需要保证不返回栈上的空间，不然调用Marshal.PtrToStringAnsi访问到非法内存的时候，程序会直接崩溃。
2. 目前用string类型来接收返回结构体的内容，程序会崩溃，出现double free之类的报错。使用IntPtr的形式手动进行Marshal转换，并拷贝内存数据到C#对象。
#### C++部分接口定义
```cpp
typedef struct symbol_values
{
    uint32_t lineNum;
    const char* srcPath;
    const char* symName;
} symbol_values_t;

OCSD_C_API symbol_values_t BstGetSymbolInfo(char *elfPath, uint64_t aads);
```
#### C#接口和调用示例
```csharp
[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
public struct symbol_info_t
{
    public UInt32 line_num;
    //[MarshalAs(UnmanagedType.LPStr)]
    //public string src_path;
    //[MarshalAs(UnmanagedType.LPStr)]
    //public string sym_name;
    public IntPtr src_path; // FIXME: 用string测试总是会崩溃，目前用 ptr形式可以正常工作
    public IntPtr sym_name;
}

// C++ API warpper
public static class DllWarpper
{
#if LINUX64
    [DllImport("loadsyms", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    //[return: MarshalAs(UnmanagedType.LPStruct)]
#else
    [DllImport("libloadsyms.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
#endif
    public static extern symbol_info_t BstGetSymbolInfo(string elf_path, UInt64 addrs);
}

public static void Main(string[] args)
{
    symbol_info_t symbol = DllWarpper.BstGetSymbolInfo(elf_path, offset);
    string src_path = Marshal.PtrToStringAnsi(symbol.src_path); // 拷贝IntPtr指向的ansi字符串到string
    string sym_name = Marshal.PtrToStringAnsi(symbol.sym_name);
    Console.WriteLine("line_num: {0}, src_path: {1}, {2}", symbol.line_num, src_path, sym_name);
}
```
### 结构体内部嵌套结构体指针
#### 示例一
##### C++部分接口定义
```cpp
#ifdef __cplusplus
extern "C" {
#endif

typedef struct code_info
{
    uint64_t code_addr;
    uint64_t offset;
    uint64_t src_code_line;
    const char* symbol_name;
    const char* src_code_path;
}code_info_t;

typedef struct symbol_infos
{
    uint64_t symbol_start_addr;
    uint64_t symbol_end_addr;
    uint32_t symbol_offset;
    uint64_t symbol_size;
    uint64_t src_code_start_line;
    uint64_t src_code_end_line;
    uint32_t is_sharelib;
    uint32_t is_success;
    const char *symbol_name;
    const char *src_code_path;
    const char* error_msg;
    // void* code_infos;
    uint32_t code_info_size;
    code_info_t *code_infos;
} symbol_infos_t;

/*
 * @brief get symbol start-end info
 * @return
 */
OCSD_C_API symbol_infos_t get_symbol_infos(cs_loadsyms_handle_t handle,
										   const char *symbol_name,
										   uint64_t symbol_start_addr);

OCSD_C_API int get_symbol_infos2(cs_loadsyms_handle_t handle,
								 const char *symbol_name,
								 uint64_t symbol_start_addr,
								 symbol_infos_t *symbol_infos);

#ifdef __cplusplus
}
#endif
```
##### C#接口和调用示例
```csharp
[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
public struct Code_Info
{
    public UInt64 code_addr;
    public UInt64 offset;
    public UInt64 src_code_line;
    [MarshalAs(UnmanagedType.LPStr)]
    public string symbol_name;
    [MarshalAs(UnmanagedType.LPStr)]
    public string src_code_path;
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
public struct Symbol_Infos
{
    public UInt64 symbol_start_addr;
    public UInt64 symbol_end_addr;
    public UInt32 symbol_offset;
    public UInt64 symbol_size;
    public UInt64 src_code_start_line;
    public UInt64 src_code_end_line;
    public UInt32 is_sharelib;
    public UInt32 is_success;
    [MarshalAs(UnmanagedType.LPStr)]
    public string symbol_name;
    [MarshalAs(UnmanagedType.LPStr)]
    public string src_code_path;
    [MarshalAs(UnmanagedType.LPStr)]
    public string error_msg;
    public Int32 code_info_size;
    public IntPtr code_infos;
}

// C++ API warpper
public static class DllWarpper
{
#if LINUX64
    [DllImport("loadsyms", CallingConvention = CallingConvention.StdCall)]
#else
    [DllImport("libloadsyms.dll", CallingConvention = CallingConvention.StdCall)]
#endif
    // public extern static string get_callchain(string project_id);
    public extern static Symbol_Infos get_symbol_infos(IntPtr handle, string symbol_name, UInt64 symbol_start_addr);

#if LINUX64
    [DllImport("loadsyms", CallingConvention = CallingConvention.StdCall)]
#else
    [DllImport("libloadsyms.dll", CallingConvention = CallingConvention.StdCall)]
#endif
    public extern static int get_symbol_infos2(IntPtr handle, string symbol_name, UInt64 symbol_start_addr, ref Symbol_Infos symbol_infos);
}

public static void Main(string[] args)
{
    // 直接返回结构体
    Symbol_Infos infos = DllWarpper.get_symbol_infos(IntPtr.Zero, "hello", 0x1234);
    if (infos.is_success != 0)
    {
        IntPtr current = infos.code_infos;
        Int32 size = infos.code_info_size;
        Int32 total_size = size * Marshal.SizeOf<Code_Info>();

        Code_Info[] code_infos = new Code_Info[size];
        for (int i = 0; i < size; i++)
        {
            code_infos[i] = (Code_Info)Marshal.PtrToStructure(current, typeof(Code_Info)); // copy to code_infos[i]
            current = (IntPtr)((long)current + Marshal.SizeOf<Code_Info>()); // update offset to next code_info
            Console.WriteLine("Element {0}: {1} {2} {3}", i, code_infos[i].code_addr,
                code_infos[i].symbol_name, code_infos[i].src_code_path);
        }
    }

    // ref test，引用C#分配的结构体在，作为C++接口的指针实参
    Symbol_Infos symbol_Infos = new Symbol_Infos();
    int ret = DllWarpper.get_symbol_infos2(IntPtr.Zero, "hello", 0x1234, ref symbol_Infos);
    if (ret == 0)
    {
        IntPtr current = symbol_Infos.code_infos;
        Int32 size = symbol_Infos.code_info_size;
        Int32 total_size = size * Marshal.SizeOf<Code_Info>();

        Code_Info[] code_infos = new Code_Info[size];
        for (int i = 0; i < size; i++)
        {
            code_infos[i] = (Code_Info)Marshal.PtrToStructure(current, typeof(Code_Info)); // copy to code_infos[i]
            //Marshal.DestroyStructure(current, typeof(Code_Info));

            current = (IntPtr)((long)current + Marshal.SizeOf<Code_Info>());

            Console.WriteLine("Element {0}: {1} {2} {3}", i, code_infos[i].code_addr,
                code_infos[i].symbol_name, code_infos[i].src_code_path);
        }
    }
    else
    {
        Console.WriteLine("failed to call!");
    }    
}
```
#### 示例二
##### C++部分接口定义
```csharp
#ifdef __cplusplus
extern "C" {
#endif

typedef struct load_code_info
{
    uint64_t code_addr;
    uint64_t offset;
    uint64_t src_code_line;
    const char*	src_code_path;
    const char*	symbol_name;
} load_code_info_t;


typedef struct load_symbol_infos
{
    uint64_t symbol_start_addr;
    uint64_t symbol_end_addr;
    uint32_t symbol_offset;
    uint64_t symbol_size;
    uint64_t src_code_start_line;
    uint64_t src_code_end_line;
    uint32_t is_sharelib;
    uint32_t is_success;
    char*	symbol_name;
    char*	src_code_path;
    char* error_msg;
    load_code_info_t *code_infos; 
    uint32_t code_infos_num;
} load_symbol_infos_t;

/*
 * @brief get symbol start-end info
 * @param elf_name elf路径字符串，是一个全路径，例如 c:/a.so
 * @return 0 success, 1 error
 */
OCSD_C_API load_symbol_infos_t get_symbol_info(cs_loadsyms_handle_t handle,
											   const char* elf_name,
											   const char* symbol_name,
											   uint64_t symbol_start_addr);
#ifdef __cplusplus
}
#endif
```
##### C#接口和调用示例
```csharp
[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
public struct Load_code_info
{
    public UInt64 code_addr;
    public UInt64 offset;
    public UInt64 src_code_line;
    //[MarshalAs(UnmanagedType.LPStr)]
    //public string src_code_path;
    //[MarshalAs(UnmanagedType.LPStr)]
    //public string symbol_name;

    // FIXME: 用string测试总是会崩溃，目前用ptr形式可以正常工作
    public IntPtr src_code_path;
    public IntPtr symbol_name;
}
[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
public struct Load_symbol_infos
{
    public UInt64 symbol_start_addr;
    public UInt64 symbol_end_addr;
    public UInt32 symbol_offset;
    public UInt64 symbol_size;
    public UInt64 src_code_start_line;
    public UInt64 src_code_end_line;
    public UInt32 is_sharelib; // 0表示不是sharelib，非0表示是
    public UInt32 is_Success;  // 0表示不成功，非0表示成功
    //[MarshalAs(UnmanagedType.LPStr)]
    //public string symbol_name;
    //[MarshalAs(UnmanagedType.LPStr)]
    //public string src_code_path;
    //[MarshalAs(UnmanagedType.LPStr)]
    //public string error_msg;

    // FIXME: 用string测试总是会崩溃，目前用ptr形式可以正常工作
    public IntPtr symbol_name;
    public IntPtr src_code_path;
    public IntPtr error_msg;
    public IntPtr codePtr; // 指向Load_code_info数组，数组大小为code_info_count
    public UInt32 code_info_count;
}

#if LINUX64
    [DllImport("Meta/linux/lib/HardwareAssistedTraceTool/libloadsyms.so", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
#else
    [DllImport("Meta/windows/lib/HardwareAssistedTraceTool/libloadsyms.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
#endif
    public extern static Load_symbol_infos get_symbol_info(IntPtr cs_loadsyms_handle, string elf_filePath, string symbol_name, ulong symbol_start_addr);

public static void Main(string[] args)
{
    Load_symbol_infos symbolInfo = get_symbol_info(project.cs_elf_handle, waiter.ElfPath, waiter.SymbolName, waiter.SymbolAddr);

    // 转换成C#空间字符串
    string sysmbol_name = symbolInfo.symbol_name == IntPtr.Zero ? "$x" : Marshal.PtrToStringAnsi(symbolInfo.symbol_name);
    string src_code_path = symbolInfo.src_code_path == IntPtr.Zero ? "$x" : Marshal.PtrToStringAnsi(symbolInfo.src_code_path);
    string err_msg = symbolInfo.error_msg == IntPtr.Zero ? "null" : Marshal.PtrToStringAnsi(symbolInfo.error_msg);
    Console.WriteLine($"sysmobl_name = {sysmbol_name}, src_code_path={src_code_path}, error_msg={err_msg}");

    long code_info_size = Marshal.SizeOf<Load_code_info>();
    IntPtr current = symbolInfo.codePtr;
    Load_code_info[] code_Infos = new Load_code_info[symbolInfo.code_info_count];
    for (int i = 0; i < symbolInfo.code_info_count; i++)
    {
        // 转换成C#空间的结构体
        code_Infos[i] = (Load_code_info)Marshal.PtrToStructure(current, typeof(Load_code_info)); // copy to code_infos[i]
        current = (IntPtr)((long)current + code_info_size); // update offset to next code_info
    }
}
```
## 调试方法
### 库或者依赖库找不到
![image.png](https://cdn.nlark.com/yuque/0/2023/png/2620787/1685065989325-8404c659-55ce-4652-a229-486b669c28a2.png#averageHue=%23282c34&clientId=u8e0cf8cc-c5b5-4&from=paste&height=530&id=u0c749482&originHeight=530&originWidth=1042&originalType=binary&ratio=1&rotation=0&showTitle=false&size=55256&status=done&style=none&taskId=uc45b62c5-53c0-4a9e-a9af-c965b593591&title=&width=1042)
#### 解决办法 - linux64
在linux平台上，C#无法正确处理依赖库的链，并自动加载，需要手动加载所有依赖的库，如下示例代码：
```csharp
// C++ API warpper
public static class DllWarpper
{
    // used for linux platform
    public const int RTLD_LAZY = 0x00001; //Only resolve symbols as needed
    public const int RTLD_GLOBAL = 0x00100; //Make symbols available to libraries loaded later

    // WARNNING: MUST CALL THIS API BEFORE ANY INVOKE!!!
    public static void initialize_envirnment(System.PlatformID platformID)
    {
        if (platformID.ToString().Contains("Win"))
        {
            // NOTE: win32 platform do nothing, but all dll must in workdir
            Console.WriteLine("Initailized enviornment for win platform.");
        }
        else
        {
            // NOTE: load all reference library, only need in linux64, linux32 not tested
            // 通过加载依赖链库，实现依赖的符号寻址，注意：必须使用RTLD_LAZY|RTLD_GLOBAL才能正确引用
            var ret = dlopen("libiberty.so", RTLD_LAZY | RTLD_GLOBAL);
            var ret2 = dlopen("libbfd-2.38.so", RTLD_LAZY | RTLD_GLOBAL);
            var ret3 = dlopen("libsqlite3.so.0.8.6", RTLD_LAZY | RTLD_GLOBAL);
            var ret4 = dlopen("libopencsd.so.1.4.0", RTLD_LAZY | RTLD_GLOBAL);
            var ret5 = dlopen("libsnapshot_parser.so", RTLD_LAZY | RTLD_GLOBAL);
            var ret6 = dlopen("libloadsyms.so", RTLD_LAZY | RTLD_GLOBAL);
            var ret7 = dlopen("libcsdecode.so", RTLD_LAZY | RTLD_GLOBAL);
            Console.WriteLine("Initailized enviornment for linux platform.");
        }
    }

    /**************************************************************************************************/
    // libdl.so, only used for linux platform
    // on linux, c# marshel will search {name}&lib{name}.so, so just set name as dl, will search libdl.so
    [DllImport("dl", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern IntPtr dlopen(string path, Int32 mode = RTLD_LAZY | RTLD_GLOBAL);
}
```
#### 解决办法 - win32
在windows平台，C#可以找到依赖的库，只需要用mingw32的编译器进行编译，此处采用的msys2的mingw32环境，默认的C库为msvcrt，故可以在x86或者x64的windows上运行。
```csharp
// C++ API warpper
public static class DllWarpper
{
    /***************************************************************************************************/
    // libcsdecode.so or libcsdecode.dll
    #if LINUX64
        // 经测试，在linux平台，有时候运行也需要依赖库全名称，具体原因未分析，改用全名称。
        // [DllImport("csdecode", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        [DllImport("libcsdecode.so", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    #else
        [DllImport("libcsdecode.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    #endif
        public static extern void init_nanlog(string log_dir, string log_file);
}
```
注意：在win平台，引用库的名称（即：DllImport方法的arg1）需要和依赖库的名称完全一致。<br />推荐在linux平台也完全一致。
### C++接口需要utf-8字符串
c++接口sqlite3_open需要以UTF-8编码的路径作为参数，而在C#中默认编码是本地地区对应的字符编码，需要手动指定转换接口类型。示例如下：
```csharp
[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
public struct trace_params_t
{
    /* 
     * 作为结构体入参，不用UnmanagedType.LPStr修饰也可以正常工作,CharSet.Ansi已经指定了为LPStr
     * 注意：
     *   1. 针对必须以UTF-8编码作为参数的部分，必须使用UnmanagedType.LPUTF8Str！！！
     *   2. 默认为LPStr进行Marshal的时候，传递到C/C++接口部分的时候，是以\0结尾的local编码对应的字符数组，
     *      如windows上中文版默认编码为GB2312，C/C++接口部分收到的值为GB2312的byte序列
     */
    public string project_id;
    [MarshalAs(UnmanagedType.LPUTF8Str)]
    public string sqlit_db;
    public string table_name;
    public string map_name;
    public string snapshot_dir;
    public UInt32 sys_type;
}
```
### C#接口调用C++接口程序崩溃
原始代码如下：
```csharp
[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
public struct symbol_info_t
{
    public UInt32 line_num;
    [MarshalAs(UnmanagedType.LPStr)]
    public string src_path;
    [MarshalAs(UnmanagedType.LPStr)]
    public string sym_name;
    // public IntPtr src_path; // FIXME: 用string测试总是会崩溃，目前用 ptr形式可以正常工作
    // public IntPtr sym_name;
}

[DllImport("libloadsyms.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
public static extern symbol_info_t BstGetSymbolInfo(string elf_path, UInt64 addrs);
```
C++ dll接口返回结构体给C#，然后结构体内部包含字符串的时候，调试在linux平台不崩溃，但是在windows平台每次都崩溃。排查发现，同样结构体类型，作为C#调用接口的参数的时候，就可以正常运行，会是什么问题呢？下面是针对Marshal返回参数中转换可能遇到的问题作总结。
#### 可能存在的问题
##### C++字符指针指向栈上空间
C++字符指针指向栈上的空间，如临时变量，那么在函数接口调用返回后，便会被释放调，这时候回到C#空间后，直接去当ANSI风格的字符串去遍历内部的元素的时候，便会踩到非法空间的东西，极有可能会导致程序崩溃（跟操作系统内存管理有关）。
##### C++字符指针未以'\0'结尾
默认C++字符都是以ANSI编码的，时长会在接口DllImport的属性中添加CharSet = CharSet.Ansi，这样的规定代表C#和C++之间参数传递或转换，遇到C#这边的字符串，会以'\0'结尾，然后拷贝到C++的内存空间。针对返回参数也是如此，如果C++部分返回的字符指针地址为非法地址，或者没有以'\0'结尾，那么当C#空间以CharSet = CharSet.Ansi规则进行参数拷贝的时候，会遍历知道遇到'\0'，那么必然会访问到非法内存，极有可能导致程序崩溃。
##### Marshal转换结构体内包含字符指针
经过测试发现，函数返回结构体，然后C#内部转换后，程序会直接崩溃，但是采用IntPtr的形式去接收C++参数中的字符指针，然后再调用下面示例代码，去拷贝未管理的内存到C#空间，便不会报错，具体原因尚不清楚。
```csharp
[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
public struct symbol_info_t
{
    public UInt32 line_num;
    public IntPtr src_path; // FIXME: 用string测试总是会崩溃，目前用 ptr形式可以正常工作
    public IntPtr sym_name;
}

string src_path = Marshal.PtrToStringAnsi(symbol.src_path)
```

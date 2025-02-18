using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Algorithm
{
    internal class Program
    {

        /// <summary>
        /// 自定义方法快1倍
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            var source = new string[] { "apple", "banana", "pear", "watermelon", "strawberry" };
            var target = new string[] { "pear", "watermelon", "strawberry", "orange", "litchi" };
            Stopwatch sw = Stopwatch.StartNew();
            var set = GetSetDiff(source, target);
            sw.Stop();
            Console.WriteLine("Time1:"+sw.ElapsedTicks);

            Stopwatch sw1 = Stopwatch.StartNew();
            var intersection = source.Intersect(target);
            var union = source.Union(target);
            var difference1 = source.Except(target);
            var difference2 = target.Except(source);
            sw1.Stop();
            Console.WriteLine("Time2:" + sw1.ElapsedTicks);
            var all = new List<string>();//并
            var add = new List<string>();//source和target的差集
            var del = new List<string>();//target和source的差集
            var both = new List<string>();//交
            foreach (var s in set)
            {
                if (s.Value == 1)
                {
                    add.Add(s.Key);
                }
                else if (s.Value == 0)
                {
                    both.Add(s.Key);
                }
                else if(s.Value == -1)
                {
                    del.Add(s.Key);
                }
                all.Add(s.Key);
            }
            Console.WriteLine("source和target的差集");
            foreach (var s in add)
            {
                Console.Write(s + " ");
            }
            Console.WriteLine();
            Console.WriteLine("target和source的差集");
            foreach (var s in del)
            {
                Console.Write(s + " ");
            }
            Console.WriteLine();
            Console.WriteLine("target和source的交集");
            foreach (var s in both)
            {
                Console.Write(s + " ");
            }
            Console.WriteLine();
            Console.WriteLine("target和source的并集");
            foreach (var s in all)
            {
                Console.Write(s + " ");
            }
            Console.WriteLine();

            Console.ReadLine();
        }

        static Dictionary<string, int> GetSetDiff(string[] source, string[] target)
        {
            Dictionary<string, int> diff = new Dictionary<string, int>();
            foreach (var i in source)
            {
                diff.Add(i, 1);//source和target的差集
            }
            foreach (var i in target)
            {
                if (diff.ContainsKey(i))
                {
                    diff[i] = 0;//交集
                }
                else
                {
                    diff[i] = -1;//target和source的差集
                }
            }
            return diff;
        }
    }
}

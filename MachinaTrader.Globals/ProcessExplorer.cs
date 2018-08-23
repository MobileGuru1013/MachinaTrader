using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace MachinaTrader.Globals
{
    public class GetProcessesToolhelp32
    {
        public struct ProcessEntry32
        {
            public uint dwSize;
            public uint cntUsage;
            public uint th32ProcessID;
            public IntPtr th32DefaultHeapID;
            public uint th32ModuleID;
            public uint cntThreads;
            public uint th32ParentProcessID;
            public int pcPriClassBase;
            public uint dwFlags;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szExeFile;
        }

        [DllImport("KERNEL32.DLL ")]
        public static extern IntPtr CreateToolhelp32Snapshot(uint flags, uint processid);
        [DllImport("KERNEL32.DLL ")]
        public static extern int CloseHandle(IntPtr handle);
        [DllImport("KERNEL32.DLL ")]
        public static extern int Process32First(IntPtr handle, ref ProcessEntry32 pe);
        [DllImport("KERNEL32.DLL ")]
        public static extern int Process32Next(IntPtr handle, ref ProcessEntry32 pe);

        //Return ProcessList -> Dont change GlobalVariables
        public static List<string> GetListSimple()
        {
            IntPtr handle = CreateToolhelp32Snapshot(0x2, 0);
            List<string> newProcessList = new List<string>();

            if ((int)handle > 0)
            {
                List<ProcessEntry32> list = new List<ProcessEntry32>();
                ProcessEntry32 pe32 = new ProcessEntry32();
                pe32.dwSize = (uint)Marshal.SizeOf(pe32);
                int bMore = Process32First(handle, ref pe32);
                while (bMore == 1)
                {
                    IntPtr temp = Marshal.AllocHGlobal((int)pe32.dwSize);
                    Marshal.StructureToPtr(pe32, temp, true);
                    ProcessEntry32 pe = Marshal.PtrToStructure<ProcessEntry32>(temp);
                    Marshal.FreeHGlobal(temp);
                    list.Add(pe);
                    bMore = Process32Next(handle, ref pe32);
                }
                CloseHandle(handle);

                //Create temp List to compare running tasks
                foreach (ProcessEntry32 p in list)
                {
                    newProcessList.Add(p.szExeFile.ToLower());
                }
            }
            return newProcessList;
        }


    }
}

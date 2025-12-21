using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace dyncompressor
{
    public static class FileFolderDialog
    {
        [ComImport]
        [Guid("DC1C5A9C-E88A-4DDE-A5A1-60F82A20AEF7")]
        [ClassInterface(ClassInterfaceType.None)]
        private class FileOpenDialog { }

        [ComImport]
        [Guid("42f85136-db7e-439c-85f1-e4075d135fc8")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IFileOpenDialog
        {
            [PreserveSig] int Show(IntPtr parent);
            void SetFileTypes(); // not used
            void SetFileTypeIndex(uint iFileType);
            void GetFileTypeIndex(out uint piFileType);
            void Advise(); void Unadvise();
            void SetOptions(uint fos);
            void GetOptions(out uint fos);
            void SetDefaultFolder(IntPtr psi);
            void SetFolder(IntPtr psi);
            void GetFolder(out IntPtr ppsi);
            void GetCurrentSelection(out IntPtr ppsi);
            void SetFileName([MarshalAs(UnmanagedType.LPWStr)] string pszName);
            void GetFileName(out IntPtr pszName);
            void SetTitle([MarshalAs(UnmanagedType.LPWStr)] string pszTitle);
            void SetOkButtonLabel([MarshalAs(UnmanagedType.LPWStr)] string pszText);
            void SetFileNameLabel([MarshalAs(UnmanagedType.LPWStr)] string pszLabel);
            void GetResult(out IntPtr ppsi);
            void AddPlace(IntPtr psi, int fdap);
            void SetDefaultExtension([MarshalAs(UnmanagedType.LPWStr)] string pszDefaultExtension);
            void Close(int hr);
            void SetClientGuid(); void ClearClientData();
            void SetFilter(IntPtr pFilter);
            void GetResults(out IntPtr ppenum);
            void GetSelectedItems(out IntPtr ppsai);
        }

        [ComImport]
        [Guid("b4db1657-70d7-485e-8e3e-6fcb5a5c18bc")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IShellItemArray
        {
            void BindToHandler(); void GetPropertyStore(); void GetPropertyDescriptionList();
            void GetAttributes(); void GetCount(out uint pdwNumItems);
            void GetItemAt(uint dwIndex, out IntPtr ppsi);
            void EnumItems();
        }

        [ComImport]
        [Guid("43826D1E-E718-42EE-BC55-A1E261C37BFE")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IShellItem
        {
            void BindToHandler(); void GetParent();
            void GetDisplayName(uint sigdnName, out IntPtr ppszName);
            void GetAttributes(); void Compare();
        }

        private const uint FOS_PICKFOLDERS = 0x20;
        private const uint FOS_FORCEFILESYSTEM = 0x40;
        private const uint FOS_ALLOWMULTISELECT = 0x200;

        private const uint SIGDN_FILESYSPATH = 0x80058000;

        public static List<string> ShowDialog()
        {
            var results = new List<string>();
            var dialog = (IFileOpenDialog)new FileOpenDialog();

            // Enable selecting both files and folders
            dialog.SetOptions(FOS_PICKFOLDERS | FOS_FORCEFILESYSTEM | FOS_ALLOWMULTISELECT);

            if (dialog.Show(IntPtr.Zero) == 0)
            {
                dialog.GetResults(out var ppenum);
                var items = (IShellItemArray)Marshal.GetObjectForIUnknown(ppenum);

                items.GetCount(out var count);
                for (uint i = 0; i < count; i++)
                {
                    items.GetItemAt(i, out var ppsi);
                    var item = (IShellItem)Marshal.GetObjectForIUnknown(ppsi);

                    item.GetDisplayName(SIGDN_FILESYSPATH, out var pszName);
                    string path = Marshal.PtrToStringUni(pszName);
                    Marshal.FreeCoTaskMem(pszName);

                    results.Add(path);
                }
            }

            return results;
        }
    }
}

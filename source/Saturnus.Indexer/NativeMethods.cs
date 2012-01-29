#region License

// 
// Copyright (c) 2012, Saturnus Project Contributors
// 
// Dual-licensed under the Apache License, Version 2.0, and the Microsoft Public License (Ms-PL).
// See the file LICENSE.txt for details.
// 

#endregion

using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Saturnus.Indexer
{
    internal class NativeMethods
    {
        /// Return Type: HICON->HICON__*
        ///hInst: HINSTANCE->HINSTANCE__*
        ///lpszExeFileName: LPCSTR->CHAR*
        ///nIconIndex: UINT->unsigned int
        [DllImport( NativeConstants.Shell32, EntryPoint = NativeConstants.ExtractIconA,
                CallingConvention = CallingConvention.StdCall )]
        public static extern IntPtr ExtractIconA( IntPtr hInst,
                                                  [In] [MarshalAs( UnmanagedType.LPStr )] string lpszExeFileName,
                                                  uint nIconIndex );

        /// Return Type: HICON->HICON__*
        ///hInst: HINSTANCE->HINSTANCE__*
        ///lpszExeFileName: LPCWSTR->WCHAR*
        ///nIconIndex: UINT->unsigned int
        [DllImport( NativeConstants.Shell32, EntryPoint = NativeConstants.ExtractIconW,
                CallingConvention = CallingConvention.StdCall )]
        public static extern IntPtr ExtractIconW( IntPtr hInst,
                                                  [In] [MarshalAs( UnmanagedType.LPWStr )] string lpszExeFileName,
                                                  uint nIconIndex );


        /// Return Type: UINT->unsigned int
        ///lpszFile: LPCSTR->CHAR*
        ///nIconIndex: int
        ///phiconLarge: HICON*
        ///phiconSmall: HICON*
        ///nIcons: UINT->unsigned int
        [DllImport( NativeConstants.Shell32, EntryPoint = NativeConstants.ExtractIconExA,
                CallingConvention = CallingConvention.StdCall )]
        public static extern uint ExtractIconExA( [In] [MarshalAs( UnmanagedType.LPStr )] string lpszFile,
                                                  int nIconIndex,
                                                  ref IntPtr phiconLarge,
                                                  ref IntPtr phiconSmall,
                                                  uint nIcons );

        /// Return Type: UINT->unsigned int
        ///lpszFile: LPCWSTR->WCHAR*
        ///nIconIndex: int
        ///phiconLarge: HICON*
        ///phiconSmall: HICON*
        ///nIcons: UINT->unsigned int
        [DllImport( NativeConstants.Shell32, EntryPoint = NativeConstants.ExtractIconExW,
                CallingConvention = CallingConvention.StdCall )]
        public static extern uint ExtractIconExW( [In] [MarshalAs( UnmanagedType.LPWStr )] string lpszFile,
                                                  int nIconIndex,
                                                  ref IntPtr phiconLarge,
                                                  ref IntPtr phiconSmall,
                                                  uint nIcons );

        /// Return Type: HICON->HICON__*
        ///hInst: HINSTANCE->HINSTANCE__*
        ///lpIconPath: LPSTR->CHAR*
        ///lpiIcon: LPWORD->WORD*
        [DllImport( NativeConstants.Shell32, EntryPoint = NativeConstants.ExtractAssociatedIconA,
                CallingConvention = CallingConvention.StdCall
                )]
        public static extern IntPtr ExtractAssociatedIconA( IntPtr hInst,
                                                            [MarshalAs( UnmanagedType.LPStr )] StringBuilder lpIconPath,
                                                            ref ushort lpiIcon );

        /// Return Type: HICON->HICON__*
        ///hInst: HINSTANCE->HINSTANCE__*
        ///lpIconPath: LPWSTR->WCHAR*
        ///lpiIcon: LPWORD->WORD*
        [DllImport( NativeConstants.Shell32, EntryPoint = NativeConstants.ExtractAssociatedIconW,
                CallingConvention = CallingConvention.StdCall
                )]
        public static extern IntPtr ExtractAssociatedIconW( IntPtr hInst,
                                                            [MarshalAs( UnmanagedType.LPWStr )] StringBuilder lpIconPath,
                                                            ref ushort lpiIcon );

        /// Return Type: BOOL->int
        /// hIcon: HICON->HICON__*
        [DllImport( NativeConstants.User32Dll, EntryPoint = NativeConstants.DestroyIcon )]
        [return : MarshalAs( UnmanagedType.Bool )]
        public static extern bool DestroyIcon( [In] IntPtr hIcon );
    }
}
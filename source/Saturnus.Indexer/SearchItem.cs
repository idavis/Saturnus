#region License

// 
// Copyright (c) 2012, Saturnus Project Contributors
// 
// Dual-licensed under the Apache License, Version 2.0, and the Microsoft Public License (Ms-PL).
// See the file LICENSE.txt for details.
// 

#endregion

using System;

namespace Saturnus.Indexer
{
    public class SearchItem
    {
        public string FileName { get; set; }
        public string FullPath { get; set; }
        public int Size { get; set; }
        public DateTime Modified { get; set; }
        public float Score { get; set; }
    }
}
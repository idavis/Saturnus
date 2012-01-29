#region License

// 
// Copyright (c) 2012, Saturnus Project Contributors
// 
// Dual-licensed under the Apache License, Version 2.0, and the Microsoft Public License (Ms-PL).
// See the file LICENSE.txt for details.
// 

#endregion

using System.Windows.Controls;

namespace Saturnus
{
    public partial class ShellView : UserControl
    {
        public ShellView()
        {
            DataContext = new ShellViewModel();

            InitializeComponent();
        }
    }
}
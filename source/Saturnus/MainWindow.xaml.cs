#region License

// 
// Copyright (c) 2012, Saturnus Project Contributors
// 
// Dual-licensed under the Apache License, Version 2.0, and the Microsoft Public License (Ms-PL).
// See the file LICENSE.txt for details.
// 

#endregion

using System;
using System.Windows;
using Saturnus.ViewModels;

namespace Saturnus
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            ViewModel = new MainWindowViewModel();
            InitializeComponent();
        }

        private MainWindowViewModel _ViewModel;
        public MainWindowViewModel ViewModel
        {
            get { return _ViewModel; }
            protected set { _ViewModel = value; }
        }
    }
}

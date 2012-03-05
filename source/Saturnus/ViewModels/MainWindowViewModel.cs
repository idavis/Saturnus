#region License

// 
// Copyright (c) 2012, Saturnus Project Contributors
// 
// Dual-licensed under the Apache License, Version 2.0, and the Microsoft Public License (Ms-PL).
// See the file LICENSE.txt for details.
// 

#endregion

using ReactiveUI;
using Saturnus.Indexer;

namespace Saturnus.ViewModels
{
    public class MainWindowViewModel : ReactiveObject
    {
        static MainWindowViewModel()
        {
            IconExtractor.Initialize();
        }

        public MainWindowViewModel(ShellViewModel shellViewModel)
        {
            ShellViewModel = shellViewModel;
        }

        public MainWindowViewModel()
            : this(new ShellViewModel())
        {
        }

        public ShellViewModel ShellViewModel { get; protected set; }
    }
}
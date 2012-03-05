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
            _ShellViewModel = shellViewModel;
        }

        public MainWindowViewModel()
            : this(new ShellViewModel())
        {
        }

        private ShellViewModel _ShellViewModel;
        public ShellViewModel ShellViewModel
        {
            get { return _ShellViewModel; }
            protected set
            {
                this.RaiseAndSetIfChanged(x => x.ShellViewModel, value);
            }
        }
    }
}
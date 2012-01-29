#region License

// 
// Copyright (c) 2012, Saturnus Project Contributors
// 
// Dual-licensed under the Apache License, Version 2.0, and the Microsoft Public License (Ms-PL).
// See the file LICENSE.txt for details.
// 

#endregion

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive.Linq;
using System.Threading.Tasks;
using ReactiveUI;
using Saturnus.Indexer;

namespace Saturnus
{
    public class ShellViewModel : ReactiveObject, IObserver<string>
    {
        private IndexGenerator _Generator;
        private string _SearchCriteria;

        public ShellViewModel()
        {
            var task = new Task( () =>
                                 {
                                     _Generator = new IndexGenerator();
                                     _Generator.CreateIndex();
                                     _Generator.Watch();
                                 } );
            task.Start();

            this.WhenAny( item => item.SearchCriteria,
                          x => x.PropertyName )
                    .Throttle( TimeSpan.FromMilliseconds( 250 ), RxApp.DeferredScheduler )
                    .Subscribe( this );
        }

        public string SearchCriteria
        {
            get { return _SearchCriteria; }
            set
            {
                this.RaiseAndSetIfChanged( x => x.SearchCriteria, value );
                this.RaisePropertyChanged( x => x.CanSearch );
            }
        }

        public ObservableCollection<SearchItem> Items { get; set; }

        public bool CanSearch
        {
            get
            {
                return !string.IsNullOrWhiteSpace( SearchCriteria ) &&
                       !SearchCriteria.StartsWith( "*" ) &&
                       !SearchCriteria.StartsWith( "?" );
            }
        }

        #region IObserver<string> Members

        public void OnNext( string value )
        {
            Search();
        }

        public void OnError( Exception error )
        {
        }

        public void OnCompleted()
        {
        }

        #endregion

        public void Search()
        {
            if ( !CanSearch )
            {
                return;
            }
            IEnumerable<SearchItem> results = _Generator.Search( SearchCriteria );
            Items = new ObservableCollection<SearchItem>( results );
            this.RaisePropertyChanged( x => x.Items );
        }
    }
}
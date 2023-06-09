﻿using System;
using System.Collections.Specialized;
using System.ComponentModel;
using NUnit.Framework;
using WB.Core.SharedKernels.Enumerator.Utils;
using WB.Tests.Abc;
using WB.Tests.Unit.SharedKernels.Enumerator.ViewModels;

namespace WB.Tests.Unit.SharedKernels.Enumerator
{
    [TestOf(typeof (CompositeCollection<>))]
    public class CompositeCollectionTests : BaseMvvmCrossTest
    {
        [Test]
        public void when_clearing_child_collection_should_raise_items_removed_with_offset_and_items()
        {
            var items = Create.Entity.CompositeCollection<string>();

            items.Add("zero");
            var childCollection = new CompositeCollection<string> {"one", "two"};
            items.AddCollection(childCollection);
            items.Add("three");

            NotifyCollectionChangedEventArgs collectionChangedArgs = null;
            items.CollectionChanged += (sender, args) =>
            {
                if (collectionChangedArgs != null) throw new Exception("Only one event expected");
                collectionChangedArgs = args;
            };

            childCollection.Clear();

            Assert.That(collectionChangedArgs, Is.Not.Null);
            Assert.That(collectionChangedArgs.Action, Is.EqualTo(NotifyCollectionChangedAction.Remove));
            Assert.That(collectionChangedArgs.OldStartingIndex, Is.EqualTo(1));
            Assert.That(collectionChangedArgs.OldItems, Is.EquivalentTo(new[] { "one", "two" }));
        }

        [Test]
        public void should_be_able_to_get_element_from_second_collection_by_index()
        {
            var items = Create.Entity.CompositeCollection<string>();

            items.Add("zero");
            var childCollection = new CompositeCollection<string> {"one", "two"};
            items.AddCollection(childCollection);

            // Act
            var result = items[2];
            Assert.Throws<IndexOutOfRangeException>(() => { var a = items[5]; });

            // Assert
            Assert.That(result, Is.EqualTo("two"));
            Assert.That(items.Contains((object)"two"));
        }

        [Test]
        public void when_clearing_child_collection_should_raise_count_property_changed()
        {
            var items = Create.Entity.CompositeCollection<string>();

            items.Add("zero");
            var childCollection = new CompositeCollection<string> {"one", "two"};
            items.AddCollection(childCollection);
            items.Add("three");

            PropertyChangedEventArgs propertyChangedEventArgs = null;
            items.PropertyChanged += (sender, args) =>
            {
                if (propertyChangedEventArgs != null) throw new Exception("Only one event expected");
                propertyChangedEventArgs = args;
            };

            childCollection.Clear();

            Assert.That(propertyChangedEventArgs, Is.Not.Null);
            Assert.That(propertyChangedEventArgs.PropertyName, Is.EqualTo("Count"));
        }

        [Test]
        public void when_notifing_item_changed()
        {
            var items = Create.Entity.CompositeCollection<string>();

            items.Add("zero");
            items.AddCollection(new CompositeCollection<string> { "one", "two" });
            items.Add("three");

            NotifyCollectionChangedEventArgs collectionChangedArgs = null;
            items.CollectionChanged += (sender, args) => collectionChangedArgs = args;

            items.NotifyItemChanged("one");

            Assert.That(collectionChangedArgs, Is.Not.Null);
            Assert.That(collectionChangedArgs.Action, Is.EqualTo(NotifyCollectionChangedAction.Replace));
            Assert.That(collectionChangedArgs.OldStartingIndex, Is.EqualTo(1));
            Assert.That(collectionChangedArgs.NewStartingIndex, Is.EqualTo(1));
        }

        [Test]
        public void when_adding_child_collection_should_raise_collection_changed_with_new_item_in_args()
        {
            var items = Create.Entity.CompositeCollection<string>();

            items.Add("zero");
            var childCollection = new CompositeCollection<string> { "one", "two" };
            items.AddCollection(childCollection);
            items.Add("four");

            NotifyCollectionChangedEventArgs collectionChangedArgs = null;
            items.CollectionChanged += (sender, args) => collectionChangedArgs = args;

            // act
            childCollection.AddCollection(new CovariantObservableCollection<string> { "three" });

            // assert
            Assert.That(collectionChangedArgs.NewItems, Is.EquivalentTo(new[] { "three" }));
            Assert.That(collectionChangedArgs.NewStartingIndex, Is.EqualTo(3));
        }

        [Test]
        public void when_clearing_child_collection_should_update_count_of_parent()
        {
            var items = Create.Entity.CompositeCollection<string>();

            var options = new CovariantObservableCollection<string> { "item" };
            items.AddCollection(options);

            var options1 = new CovariantObservableCollection<string> {"item1"};
            items.AddCollection(options1);

            // act
            options.Clear();

            // assert
            Assert.That(items.Count, Is.EqualTo(1));
        }

        [Test]
        public void when_insert_child_collection_should_raise_collection_changed_with_new_items_and_offset_in_args()
        {
            var items = Create.Entity.CompositeCollection<string>();

            items.Add("zero");
            var childCollection = new CompositeCollection<string> { "one", "three" };
            items.AddCollection(childCollection);
            items.Add("four");

            NotifyCollectionChangedEventArgs collectionChangedArgs = null;
            items.CollectionChanged += (sender, args) => collectionChangedArgs = args;

            // act
            childCollection.InsertCollection(1, new CovariantObservableCollection<string> { "two" });

            // assert
            Assert.That(collectionChangedArgs.Action, Is.EqualTo(NotifyCollectionChangedAction.Add));
            Assert.That(collectionChangedArgs.NewItems, Is.EquivalentTo(new[] { "two" }));
            Assert.That(collectionChangedArgs.NewStartingIndex, Is.EqualTo(2));
        }

        [Test]
        public void when_removed_child_collection_should_raise_collection_changed_with_items_and_offset_in_args()
        {
            var items = Create.Entity.CompositeCollection<string>();

            items.Add("zero");
            var childCollection = new CompositeCollection<string> { "one", "three" };
            items.AddCollection(childCollection);
            items.Add("four");
            var collectionToRemove = new CovariantObservableCollection<string> { "two" };
            childCollection.InsertCollection(1, collectionToRemove);

            NotifyCollectionChangedEventArgs collectionChangedArgs = null;
            items.CollectionChanged += (sender, args) => collectionChangedArgs = args;

            // act
            childCollection.RemoveCollection(collectionToRemove);

            // assert
            Assert.That(collectionChangedArgs.Action, Is.EqualTo(NotifyCollectionChangedAction.Remove));
            Assert.That(collectionChangedArgs.OldItems, Is.EquivalentTo(new[] { "two" }));
            Assert.That(collectionChangedArgs.OldStartingIndex, Is.EqualTo(2));
        }
        
        [Test]
        public void when_removed_last_child_collection_should_raise_collection_changed_with_items_and_offset_in_args()
        {
            var items = Create.Entity.CompositeCollection<string>();

            var childCollection = new CompositeCollection<string>();
            items.AddCollection(childCollection);
            var collectionToRemove = new CovariantObservableCollection<string> { "single" };
            childCollection.AddCollection(collectionToRemove);

            NotifyCollectionChangedEventArgs collectionChangedArgs = null;
            items.CollectionChanged += (sender, args) => collectionChangedArgs = args;

            // act
            childCollection.RemoveCollection(collectionToRemove);

            // assert
            Assert.That(collectionChangedArgs.Action, Is.EqualTo(NotifyCollectionChangedAction.Remove));
            Assert.That(collectionChangedArgs.OldItems, Is.EquivalentTo(new[] { "single" }));
            Assert.That(collectionChangedArgs.OldStartingIndex, Is.EqualTo(0));
        }  
        

        [Test]
        public void when_removed_collection_from_second_level_should_raise_collection_changed_with_items_and_offset_in_args()
        {
            var firstLevel = Create.Entity.CompositeCollection<string>();

            firstLevel.AddCollection(Create.Entity.CompositeCollection<string>("1"));
            var secondLevel = new CompositeCollection<string>();
            secondLevel.Add("2");
            firstLevel.AddCollection(secondLevel);
            firstLevel.AddCollection(Create.Entity.CompositeCollection<string>("5"));
            var thirdLevel = new CovariantObservableCollection<string> { "3" };
            secondLevel.AddCollection(thirdLevel);
            secondLevel.Add("4");

            NotifyCollectionChangedEventArgs collectionChangedArgs = null;
            firstLevel.CollectionChanged += (sender, args) => collectionChangedArgs = args;

            // act
            firstLevel.RemoveCollection(secondLevel);

            // assert
            Assert.That(collectionChangedArgs.Action, Is.EqualTo(NotifyCollectionChangedAction.Remove));
            Assert.That(collectionChangedArgs.OldItems, Is.EquivalentTo(new[] { "2", "3", "4" }));
            Assert.That(collectionChangedArgs.OldStartingIndex, Is.EqualTo(1));
        }
    }
}

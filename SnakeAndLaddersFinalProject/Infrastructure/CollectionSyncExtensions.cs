using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace SnakeAndLaddersFinalProject.Infrastructure
{
    internal static class CollectionSyncExtensions
    {
        public static void SynchronizeWith<TVm, TDto>(
            this ObservableCollection<TVm> target,
            IEnumerable<TDto> source,
            Func<TVm, TDto, bool> match,
            Func<TDto, TVm> selector,
            Action<TVm, TDto> update)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            if (source == null)
            {
                return;
            }

            var sourceList = source as IList<TDto> ?? source.ToList();

            RemoveMissingItems(target, sourceList, match);
            AddOrUpdateItems(target, sourceList, match, selector, update);
        }

        private static void RemoveMissingItems<TVm, TDto>(
            ObservableCollection<TVm> target,
            IList<TDto> sourceList,
            Func<TVm, TDto, bool> match)
        {
            for (int index = target.Count - 1; index >= 0; index--)
            {
                var viewModelItem = target[index];

                bool exists = sourceList.Any(dto => match(viewModelItem, dto));
                if (!exists)
                {
                    target.RemoveAt(index);
                }
            }
        }

        private static void AddOrUpdateItems<TVm, TDto>(
            ObservableCollection<TVm> target,
            IList<TDto> sourceList,
            Func<TVm, TDto, bool> match,
            Func<TDto, TVm> selector,
            Action<TVm, TDto> update)
        {
            foreach (var dto in sourceList)
            {
                TVm existingItem = target.FirstOrDefault(vm => match(vm, dto));

                if (Equals(existingItem, default(TVm)))
                {
                    target.Add(selector(dto));
                }
                else
                {
                    update(existingItem, dto);
                }
            }
        }
    }
}

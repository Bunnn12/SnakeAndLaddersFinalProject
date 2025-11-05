using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

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

            var sourceList = new List<TDto>(source);

            for (int index = target.Count - 1; index >= 0; index--)
            {
                var viewModelItem = target[index];
                var exists = false;

                foreach (var dto in sourceList)
                {
                    if (match(viewModelItem, dto))
                    {
                        exists = true;
                        break;
                    }
                }

                if (!exists)
                {
                    target.RemoveAt(index);
                }
            }

            foreach (var dto in sourceList)
            {
                TVm found = default(TVm);

                foreach (var viewModelItem in target)
                {
                    if (match(viewModelItem, dto))
                    {
                        found = viewModelItem;
                        break;
                    }
                }

                if (Equals(found, default(TVm)))
                {
                    target.Add(selector(dto));
                }
                else
                {
                    update(found, dto);
                }
            }
        }
    }
}

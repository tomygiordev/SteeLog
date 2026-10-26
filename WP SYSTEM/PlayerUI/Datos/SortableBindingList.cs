
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;

namespace WP_Inventario.Datos 
{
    public class SortableBindingList<T> : BindingList<T>
    {
        private bool _isSorted;
        private ListSortDirection _sortDirection;
        private PropertyDescriptor _sortProperty;

        public SortableBindingList(IList<T> list) : base(list) { }
        public SortableBindingList() { }

        protected override bool SupportsSortingCore => true;
        protected override bool IsSortedCore => _isSorted;
        protected override ListSortDirection SortDirectionCore => _sortDirection;
        protected override PropertyDescriptor SortPropertyCore => _sortProperty;

        protected override void ApplySortCore(PropertyDescriptor prop, ListSortDirection direction)
        {
            var items = this.Items as List<T>;

            if (items != null)
            {
                var propertyInfo = typeof(T).GetProperty(prop.Name);
                if (propertyInfo != null)
                {
                    items.Sort((x, y) =>
                    {
                        var valueX = propertyInfo.GetValue(x);
                        var valueY = propertyInfo.GetValue(y);

                        int result;
                        if (valueX == null && valueY == null) result = 0;
                        else if (valueX == null) result = -1;
                        else if (valueY == null) result = 1;
                        else if (valueX is IComparable comp) result = comp.CompareTo(valueY);
                        else result = valueX.ToString().CompareTo(valueY.ToString());

                        return direction == ListSortDirection.Ascending ? result : -result;
                    });

                    _isSorted = true;
                    _sortDirection = direction;
                    _sortProperty = prop;

                    this.OnListChanged(new ListChangedEventArgs(ListChangedType.Reset, -1));
                }
            }
        }

        protected override void RemoveSortCore()
        {
            _isSorted = false;
            this.OnListChanged(new ListChangedEventArgs(ListChangedType.Reset, -1));
        }
    }
}
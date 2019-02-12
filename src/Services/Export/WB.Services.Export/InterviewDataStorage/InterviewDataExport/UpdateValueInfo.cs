﻿using System.Diagnostics;
using NpgsqlTypes;

namespace WB.Services.Export.InterviewDataStorage.InterviewDataExport
{
    [DebuggerDisplay("{" + nameof(ToString) + "()}")]
    public class UpdateValueInfo
    {
        public string ColumnName { get; set; }
        public object Value { get; set; }
        public NpgsqlDbType ValueType { get; set; }

        public override string ToString() => ColumnName + "-" + Value + "-" + ValueType;

        public override bool Equals(object obj)
        {
            var item = obj as UpdateValueInfo;
            if (item == null)
                return false;

            return (object.ReferenceEquals(this.ColumnName, item.ColumnName) || this.ColumnName != null && this.ColumnName.Equals(item.ColumnName))
                   && (object.ReferenceEquals(this.Value, item.Value) || this.Value != null && this.Value.Equals(item.Value))
                   && this.ValueType.Equals(item.ValueType);
        }

        public override int GetHashCode()
        {
            return this.ColumnName?.GetHashCode() ?? 0 ^ this.Value?.GetHashCode() ?? 0 ^ this.ValueType.GetHashCode();
        }
    }
}

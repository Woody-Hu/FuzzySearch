using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FuzzySearch
{
    /// <summary>
    /// 搜索对象接口
    /// </summary>
    public interface ISearchItem
    {
        /// <summary>
        /// 对象名称
        /// </summary>
        string ItemName
        { get; }
    }
}

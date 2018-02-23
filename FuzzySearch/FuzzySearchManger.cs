using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace FuzzySearch
{
    /// <summary>
    /// 模糊搜索管理器
    /// </summary>
    public class FuzzySearchManger
    {
        #region 私有字段
        /// <summary>
        /// 正则表达式空白字符
        /// </summary>
        private const string m_strUseWhiteRegex = @"\s";

        /// <summary>
        /// 使用的空白字符
        /// </summary>
        private const string m_strUseWhite = " ";

        /// <summary>
        /// 使用的空白字符串正则表达式
        /// </summary>
        private static Regex m_useWhiteRegex = new Regex(m_strUseWhiteRegex + "+");

        /// <summary>
        /// 被检索的数据源
        /// </summary>
        private Dictionary<string, List<ISearchItem>> m_dicSearchSource = new Dictionary<string, List<ISearchItem>>();

        /// <summary>
        /// 检索缓存
        /// </summary>
        private Dictionary<string, List<ISearchItem>> m_cache = new Dictionary<string, List<ISearchItem>>();

        /// <summary>
        /// 上次检索缓存
        /// </summary>
        private KeyValuePair<string, Dictionary<string,List<ISearchItem>>> m_LastSearchcache 
            = new KeyValuePair<string, Dictionary<string, List<ISearchItem>>>();

        /// <summary>
        /// 输入关键字是否使用空白分词
        /// </summary>
        private bool m_bifUseWhiteSplite = true;

        /// <summary>
        /// 分词结果是否需要全体命中
        /// </summary>
        private bool m_bifNeedAllTarget = true;

        /// <summary>
        /// 缓存上限数量
        /// </summary>
        private int m_nLimitCacheCount = 50;
        #endregion

        /// <summary>
        /// 构造
        /// </summary>
        /// <param name="inputItems">输入的数据源</param>
        /// <param name="ifNeedAllTarget">输入关键字是否使用空白分词</param>
        /// <param name="ifUseWhiteSplite">分词结果是否需要全体命中</param>
        public FuzzySearchManger(List<ISearchItem> inputItems,bool ifUseWhiteSplite = true,bool ifNeedAllTarget = true,int limitCacheCount = 50)
        {
            m_nLimitCacheCount = limitCacheCount;
            m_bifUseWhiteSplite = ifUseWhiteSplite;
            m_bifNeedAllTarget = ifNeedAllTarget;
            foreach (var oneItem in inputItems)
            {
                TryRegistOneItem(oneItem, false);
            }
        }

        /// <summary>
        /// 检索
        /// </summary>
        /// <param name="input">检索输入</param>
        /// <param name="ifUseWhiteSplite">输入关键字是否使用空白分词</param>
        /// <param name="ifNeedAllTarget">分词结果是否需要全体命中</param>
        /// <returns>检索结果</returns>
        public List<ISearchItem> Search(string input)
        {
            List<ISearchItem> returnValue = new List<ISearchItem>();

            //使用的实际搜索输入
            string realUseString;

            //获得分词关键组
            var spliteValue = GetSearchKey(input, m_bifUseWhiteSplite, out realUseString);

            //使用的检索源
            Dictionary<string, List<ISearchItem>> nowUseSoruce = m_dicSearchSource;

            List<List<ISearchItem>> searchedValue = new List<List<FuzzySearch.ISearchItem>>();

            if (null == spliteValue)
            {
                return returnValue;
            }

            //判断是否可以使用上次检索结果
            //*上次检索可以触发此次添加的数值
            if (IfCanUseLastCache(realUseString))
            {
                nowUseSoruce = m_LastSearchcache.Value;
            }


            foreach (var oneKeyValue in spliteValue)
            {
                //检索并将结果加入到缓存
                AddSearchResultToCache(oneKeyValue, nowUseSoruce);

                //缓存获取
                searchedValue.Add(m_cache[oneKeyValue]);
            }

            //准备检索结果
            PrepareSearchedValue(returnValue, searchedValue);

            //去重
            returnValue = returnValue.Distinct().ToList();

            //制作当前检索缓存
            var nowSearCache = returnValue.GroupBy(k => AdjustString(k.ItemName)).ToDictionary(k => k.Key, k => k.ToList());

            //保存上次检索缓存
            m_LastSearchcache = new KeyValuePair<string, Dictionary<string, List<ISearchItem>>>(realUseString, nowSearCache);

            return returnValue;
        }

        /// <summary>
        /// 判断是否可以使用上次检索结果
        /// *上次检索可以触发此次添加的数值
        /// </summary>
        /// <param name="realUseString">输入的关键字符</param>
        /// <returns>是否可以</returns>
        private bool IfCanUseLastCache(string realUseString)
        {
            return !string.IsNullOrWhiteSpace(m_LastSearchcache.Key) && realUseString.Contains(m_LastSearchcache.Key);
        }

        /// <summary>
        /// 注册一个数据源
        /// </summary>
        /// <param name="inputItem">输入的数据源</param>
        /// <returns>是否成功</returns>
        public bool TryRegistOneItem(ISearchItem inputItem)
        {
            return TryRegistOneItem(inputItem, true);
        }

        #region 私有方法

        /// <summary>
        /// 输入关键字检索并将检索结果添加到缓存
        /// </summary>
        /// <param name="oneKeyValue">输入的关键字</param>
        /// <param name="useSearchSource">使用的搜索源</param>
        private void AddSearchResultToCache(string oneKeyValue,Dictionary<string, List<ISearchItem>> useSearchSource)
        {
            //缓存检查
            if (!m_cache.ContainsKey(oneKeyValue))
            {
                List<ISearchItem> tempLst = new List<ISearchItem>();
                //临时正则表达式
                Regex tempRegex = new Regex(oneKeyValue);

                //正则模糊检索
                foreach (var oneKVP in useSearchSource)
                {
                    //若命中
                    if (tempRegex.IsMatch(oneKVP.Key))
                    {
                        tempLst.AddRange(oneKVP.Value);
                    }
                }
                //去重
                tempLst = tempLst.Distinct().ToList();

                m_cache.Add(oneKeyValue, tempLst);

                //缓存数量限制检查
                if (m_cache.Count > m_nLimitCacheCount)
                {
                    m_cache.Remove(m_cache.ElementAt(0).Key);
                }

            }
        }

        /// <summary>
        /// 准备检索结果
        /// </summary>
        /// <param name="returnValue">合并的检索结果</param>
        /// <param name="searchedValue">分组的检索结果</param>
        private void PrepareSearchedValue(List<ISearchItem> returnValue, List<List<ISearchItem>> searchedValue)
        {
            //数量调整
            if (searchedValue.Count == 1)
            {
                returnValue.AddRange(searchedValue[0]);
            }
            //多重关键词
            else if (searchedValue.Count >= 1)
            {
                //若需要全体命中
                if (m_bifNeedAllTarget)
                {
                    var tempsearchedValue = searchedValue[0];
                    for (int searchedValueIndex = 1; searchedValueIndex < searchedValue.Count; searchedValueIndex++)
                    {
                        tempsearchedValue = tempsearchedValue.Join(searchedValue[searchedValueIndex], k => k, k => k, (k, z) => k).ToList();
                    }

                    returnValue.AddRange(tempsearchedValue);
                }
                else
                {
                    //全部加入
                    foreach (var oneValue in searchedValue)
                    {
                        returnValue.AddRange(oneValue);
                    }
                }
            }
        }

        /// <summary>
        /// 注册一个数据源
        /// </summary>
        /// <param name="inputItem">输入的数据源</param>
        /// <param name="ifAdjustCache">是否调整检索缓存</param>
        /// <returns>是否成功</returns>
        private bool TryRegistOneItem(ISearchItem inputItem, bool ifAdjustCache)
        {
            if (null == inputItem || string.IsNullOrWhiteSpace(inputItem.ItemName))
            {
                return false;
            }

            //调整输入
            string realName = AdjustString(inputItem.ItemName);

            if (!m_dicSearchSource.ContainsKey(realName))
            {
                m_dicSearchSource.Add(realName, new List<ISearchItem>());
            }

            if (!m_dicSearchSource[realName].Contains(inputItem))
            {
                m_dicSearchSource[realName].Add(inputItem);

                //判断是否需要添加到上次检索缓存
                //*上次检索可以触发此次添加的数值
                if (IfCanUseLastCache(realName))
                {
                    if (!m_LastSearchcache.Value.ContainsKey(realName))
                    {
                        m_LastSearchcache.Value.Add(realName, new List<ISearchItem>());
                    }
                    m_LastSearchcache.Value[realName].Add(inputItem);
                }

                //若需要调整缓存
                if (ifAdjustCache)
                {
                    Regex tempRegex = null;
                    foreach (var oneCacheKVP in m_cache)
                    {
                        tempRegex = new Regex(oneCacheKVP.Key);

                        //若命中
                        if (tempRegex.IsMatch(realName))
                        {
                            //加入缓存
                            oneCacheKVP.Value.Add(inputItem);
                        }
                    }
                }

                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// 调整输入字符串
        /// </summary>
        /// <param name="inputString">输入的字符串</param>
        /// <returns>调整的结果</returns>
        private static string AdjustString(string inputString)
        {
            //去除头尾空白
            string useString = inputString.Trim();

            return m_useWhiteRegex.Replace(useString, m_strUseWhite);
        }

        /// <summary>
        /// 获得搜索的关键值
        /// </summary>
        /// <param name="inputString">输入字符串</param>
        /// <param name="ifUseWhiteSplite">是否使用空白分词</param>
        /// <returns>分词结果 可 null</returns>
        private static string[] GetSearchKey(string inputString, bool ifUseWhiteSplite, out string realUseString)
        {
            realUseString = null;
            if (string.IsNullOrWhiteSpace(inputString))
            {
                return null;
            }
            else
            {
                //调整输入
                realUseString = AdjustString(inputString);
                //分组
                if (ifUseWhiteSplite)
                {
                    return realUseString.Split(m_strUseWhite[0]);
                }
                else
                {
                    return new string[] { realUseString };
                }
         
            }
        } 
        #endregion

    }
}

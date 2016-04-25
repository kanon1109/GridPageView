using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
/// <summary>
/// 格子翻页组件
/// TODO:
/// [找到点击释放的事件]
/// [onDragEnd后翻页状态 滚动页面]
/// [隐藏和显示多余的item]
/// [添加和删除item或者页数]
/// [跳转到某一页]
/// </summary>
public class GridPageView : MonoBehaviour, IEndDragHandler
{
    //item容器
    public GameObject content;
    //滚动组件
    public GameObject scroll;
    //item的预设
    public GameObject itemPrefab;
    //是否是横向的
    private bool isHorizontal;
    //更新的回调方法
    public delegate void UpdateGridItem(GameObject item, int index, int pageIndex, bool isReload);
    //更新列表回调方法
    private UpdateGridItem m_updateItem;
    //页数
    private int _pageCount = 0;
    public int pageCount {get { return _pageCount; }}
    //一页中的格子数量
    private int cellsMaxCountInPage;
    //横向间隔
    private float gapH;
    //纵向间隔
    private float gapV;
    //元素宽度
    private float itemWidth;
    //元素高度
    private float itemHeight;
    //content的transform组件
    private RectTransform contentRectTf;
    //存放页数的列表
    private List<GameObject> pageList = null;
    //存放每页的item列表
    private List<List<GameObject>> pageItemList = null;
    //总格子数
    private int _cellCount;
    public int cellCount {get { return _cellCount; }}
    //行数(决定list的高度)
    private int rows;
    //列数(决定list的宽度)
    private int columns;
    //当前页数
    private int curPageIndex;
    public int pageIndex { get { return curPageIndex; }}
    //滚动组件
    private ScrollRect sr;
    //单页的高宽
    private Vector2 pageSize;
    //可显示的页数
    private int showPageCount = 2;
    //底部位置
    private float bottom;
    //顶部位置
    private float top;
    //左边位置
    private float left;
    //右边位置
    private float right;
    //最后一页可显示item的数量
    private int lastPageItemCount;
    //第一页的位置
    private Vector2 firstPagePos;
    /// <summary>
    /// 初始化
    /// </summary>
    /// <param name="rows">一页的行数</param>
    /// <param name="columns">一页的列数</param>
    /// <param name="cellCount">格子的总数</param>
    /// <param name="isHorizontal">是否横向</param>
    /// <param name="gapH">格子的横向间隔</param>
    /// <param name="gapV">格子的纵向间隔</param>
    /// <param name="updateItem">回调函数</param>
    public void init(int rows, 
                     int columns,
                     int cellCount, 
                     bool isHorizontal = true, 
                     float gapH = 5,
                     float gapV = 5,
                     UpdateGridItem updateItem = null)
    {
        //行列
        this.rows = rows;
        this.columns = columns;
        //一页多少格子
        this.cellsMaxCountInPage = rows * columns;
        if (cellCount < 0) cellCount = 0;
        if (this.scroll == null) return;
        if (this.content == null) return;

        this.m_updateItem = updateItem;
        this._cellCount = cellCount;
        this.isHorizontal = isHorizontal;
        this.gapH = gapH;
        this.gapV = gapV;

        this.curPageIndex = 0;

        this.itemWidth = this.itemPrefab.GetComponent<RectTransform>().sizeDelta.x;
        this.itemHeight = this.itemPrefab.GetComponent<RectTransform>().sizeDelta.y;

        //计算单页高宽
        this.pageSize = new Vector2(this.columns * (this.itemWidth + this.gapH),
                                    this.rows * (this.itemHeight + this.gapV));

        this.scroll.GetComponent<RectTransform>().sizeDelta = pageSize;
        this.sr = this.scroll.GetComponent<ScrollRect>();
        this.sr.horizontal = this.isHorizontal;
        this.sr.vertical = !this.isHorizontal;
        this.contentRectTf = this.content.GetComponent<RectTransform>();
        this.content.transform.localPosition = new Vector3(0, 0);
        this.firstPagePos = new Vector2();
        this.reloadData(cellCount);
    }

    /// <summary>
    /// 创建一个页的内容
    /// </summary>
    /// <param name="prefab">预制件</param>
    /// <param name="createPageCount">可显示的页数</param>
    private void createPageItem(GameObject prefab, int createPageCount)
    {
        if (createPageCount <= 0) return;
        if (this.pageList == null) 
            this.pageList = new List<GameObject>();

        if (this.pageItemList == null)
            this.pageItemList = new List<List<GameObject>>();

        for (int i = 0; i < createPageCount; ++i)
        {
            GameObject pageContainer = new GameObject();
            pageContainer.AddComponent<RectTransform>();
            pageContainer.AddComponent<CanvasRenderer>();
            pageContainer.transform.SetParent(this.content.gameObject.transform);
            pageContainer.GetComponent<RectTransform>().pivot = new Vector2(0, 1);
            pageContainer.GetComponent<RectTransform>().anchorMin = new Vector2(0, 1);
            pageContainer.GetComponent<RectTransform>().anchorMax = new Vector2(0, 1);
            pageContainer.GetComponent<RectTransform>().sizeDelta = this.pageSize;
            pageContainer.transform.localScale = new Vector3(1, 1, 1);

            List<GameObject> list = new List<GameObject>();
            this.pageItemList.Add(list);

            //设置每一页的位置
            pageContainer.name = "page" + i;
            this.pageList.Add(pageContainer);

            int rows = 0;
            int columns = 0;
            for (int j = 0; j < this.cellsMaxCountInPage; ++j)
            {
                GameObject item = MonoBehaviour.Instantiate(prefab, new Vector3(0, 0), new Quaternion()) as GameObject;
                item.transform.SetParent(pageContainer.transform);
                item.transform.localScale = new Vector3(1, 1, 1);
                list.Add(item);
                //排列先横向再纵向
                float x = columns * (this.itemWidth + this.gapH);
                float y = rows * -(this.itemHeight + this.gapV);
                item.transform.localPosition = new Vector3(x, y);
                columns++;
                if (columns == this.columns)
                {
                    columns = 0;
                    rows++;
                }
            }
        }
    }

    /// <summary>
    /// 重新设置数据
    /// </summary>
    /// <param name="cellCount">当前数据列表的数量</param>
    public void reloadData(int cellCount)
    {
        this.contentRectTf.DOKill();
        this.firstPagePos = this.getFirstPagePos();
        int prevPageCount = this._pageCount;
        //计算页数
        if (cellCount % this.cellsMaxCountInPage == 0)
            this._pageCount = cellCount / this.cellsMaxCountInPage;
        else
            this._pageCount = cellCount / this.cellsMaxCountInPage + 1;
        //计算最后一页的格子数量
        this.lastPageItemCount = cellCount % this.cellsMaxCountInPage;
        if (lastPageItemCount == 0) lastPageItemCount = this.cellsMaxCountInPage;
        int curLastPageIndex = this.curPageIndex + this.showPageCount - 1;
        int lastPageIndex = this._pageCount - 1;
        if (this.curPageIndex > 0 && curLastPageIndex > lastPageIndex)
        {
            //当前不在第一页 并且 最后索引位置溢出了
            int overCount = curLastPageIndex - lastPageIndex;
            this.curPageIndex -= overCount;
            //防止去除溢出后 索引为负数。
            if (this.curPageIndex < 0) this.curPageIndex = 0;
            if (this.curPageIndex == 0)
            {
                this.firstPagePos = new Vector2();
            }
            else
            {
                //补全位置
                if (!this.isHorizontal)
                    this.firstPagePos.y += this.pageSize.y * overCount;
                else
                    this.firstPagePos.x -= this.pageSize.x * overCount;
            }
        }
        //删除多余的页数
        this.removeOverPage();
        //获取最新边界
        if (this._pageCount < this.showPageCount)
            this.showPageCount = this._pageCount;
        int creatPageCount = this.showPageCount - prevPageCount;
        this.createPageItem(this.itemPrefab, creatPageCount);
        this.updateContentSize();
        this.updateBorder();
        this.layoutPage();
        this.fixContentPos();
        this.updatePageItemActive();
    }

    /// <summary>
    /// 更新滚动内容的大小
    /// </summary>
    private void updateContentSize()
    {
        Vector2 size;
        if (this.isHorizontal)
            size = new Vector2(this.pageSize.x * this._pageCount,
                               this.pageSize.y);
        else
            size = new Vector2(this.pageSize.x,
                               this.pageSize.y * this._pageCount);
        this.contentRectTf.sizeDelta = size;
    }


    /// <summary>
    /// 修正content的位置
    /// </summary>
    private void fixContentPos()
    {
        if (!this.isHorizontal)
        {
            //防止数量减少后content的位置在遮罩上面
            if (this.contentRectTf.sizeDelta.y <= this.pageSize.y)
            {
                //如果高度不够但content顶部超过scroll的顶部则content顶部归零对齐
                if (this.contentRectTf.localPosition.y > 0)
                    this.contentRectTf.localPosition = new Vector3(this.contentRectTf.localPosition.x, 0);
            }
            else
            {
                //如果高度足够但content底部超过scroll的底部则content底部对齐scroll的底部
                if (this.contentRectTf.localPosition.y - this.contentRectTf.sizeDelta.y > -this.pageSize.y)
                    this.contentRectTf.localPosition = new Vector3(this.contentRectTf.localPosition.x,
                                                                    -this.pageSize.y + this.contentRectTf.sizeDelta.y);
            }
        }
        else
        {
            //防止数量减少后content的位置在遮罩左面
            if (this.contentRectTf.sizeDelta.x <= this.pageSize.x)
            {
                if (this.contentRectTf.localPosition.x < 0)
                    this.contentRectTf.localPosition = new Vector3(0, this.contentRectTf.localPosition.y);
            }
            else
            {
                if (this.contentRectTf.localPosition.x + this.contentRectTf.sizeDelta.x < this.pageSize.x)
                    this.contentRectTf.localPosition = new Vector3(this.pageSize.x - this.contentRectTf.sizeDelta.x,
                                                                   this.contentRectTf.localPosition.y);
            }
        }
    }

    /// <summary>
    /// 拖动结束
    /// </summary>
    /// <param name="eventData"></param>
    public void OnEndDrag(PointerEventData eventData)
    {
        this.sr.StopMovement();
        GameObject pageGo = this.getPageGoByPageIndex(this.curPageIndex);
        //计算当前页未拖动时的位置
        Vector2 beforeDragPagePos = new Vector2(-this.curPageIndex * this.pageSize.x, this.curPageIndex * this.pageSize.y);
        //获取拖动后的位置
        Vector2 afterDragPagePos = this.content.transform.localPosition;
        if(this.isHorizontal)
        {
            //横向
            if (afterDragPagePos.x < beforeDragPagePos.x)
            {
                //往左拖动
                if (afterDragPagePos.x < beforeDragPagePos.x - this.pageSize.x * .5f)
                    this.contentRectTf.DOLocalMoveX(beforeDragPagePos.x - this.pageSize.x - .01f, .4f); //翻页
                else
                    this.contentRectTf.DOLocalMoveX(beforeDragPagePos.x, .4f); //回滚
            }
            else if (afterDragPagePos.x > beforeDragPagePos.x)
            {
                //往右拖动
                if (afterDragPagePos.x > beforeDragPagePos.x + this.pageSize.x * .5f)
                    this.contentRectTf.DOLocalMoveX(beforeDragPagePos.x + this.pageSize.x + .01f, .4f); //翻页
                else
                    this.contentRectTf.DOLocalMoveX(beforeDragPagePos.x, .4f); //回滚
            }
        }
        else
        {
            //纵向
            if (afterDragPagePos.y > beforeDragPagePos.y)
            {
                //向上
                if (afterDragPagePos.y > beforeDragPagePos.y + this.pageSize.y * .5f)
                    this.contentRectTf.DOLocalMoveY(beforeDragPagePos.y + this.pageSize.y + .01f, .4f); //翻页
                else
                    this.contentRectTf.DOLocalMoveY(beforeDragPagePos.y, .4f); //回滚
            }
            else if (afterDragPagePos.y < beforeDragPagePos.y)
            {
                //向下
                if (afterDragPagePos.y < beforeDragPagePos.y - this.pageSize.y * .5f)
                    this.contentRectTf.DOLocalMoveY(beforeDragPagePos.y - this.pageSize.y - .01f, .4f); //翻页
                else
                    this.contentRectTf.DOLocalMoveY(beforeDragPagePos.y, .4f); //回滚
            }
        }
    }

    /// <summary>
    /// 循环page的滚动
    /// </summary>
    private void updatePage()
    {
        if (this.pageList == null) return;
        for (int i = 0; i < this.pageList.Count; ++i)
        {
            List<GameObject> itemList = this.pageItemList[i];
            GameObject pageGo = this.pageList[i];
            Transform pageGoTr = pageGo.transform;
            Vector2 pagePos = scroll.transform.InverseTransformPoint(pageGoTr.position);
            if (this.isHorizontal)
            {
                if (pagePos.x < this.left && this.curPageIndex < this._pageCount - this.showPageCount)
                {
                    //向左循环
                    GameObject lastPageGo = this.pageList[this.pageList.Count - 1];
                    this.pageList.RemoveAt(i);
                    pageGoTr.localPosition = new Vector3(lastPageGo.transform.localPosition.x + this.pageSize.x,
                                                         pageGoTr.localPosition.y);
                    this.pageList.Add(pageGo);
                    pageGo.name = "page" + (this.curPageIndex + this.showPageCount);

                    this.pageItemList.RemoveAt(i);
                    this.pageItemList.Add(itemList);

                    this.curPageIndex++;

                    if (this.curPageIndex == this._pageCount - this.showPageCount)
                        this.setPageItemActive(itemList);
                    break;
                }
                else if (pagePos.x > this.right && this.curPageIndex > 0)
                {
                    //向右循环
                    GameObject firstPageGo = this.pageList[0];
                    this.pageList.RemoveAt(i);
                    pageGoTr.localPosition = new Vector3(firstPageGo.transform.localPosition.x - this.pageSize.x,
                                                         pageGoTr.localPosition.y);
                    this.pageList.Insert(0, pageGo);
                    pageGo.name = "page" + (this.curPageIndex + this.showPageCount);

                    this.pageItemList.RemoveAt(i);
                    this.pageItemList.Insert(0, itemList);

                    if (this.curPageIndex == this._pageCount - this.showPageCount)
                        this.setPageItemActive(itemList, false);
                    this.curPageIndex--;
                    break;
                }
            }
            else
            {
                if (pagePos.y > this.top && this.curPageIndex < this._pageCount - this.showPageCount)
                {
                    //向上循环
                    GameObject lastPageGo = this.pageList[this.pageList.Count - 1];
                    this.pageList.RemoveAt(i);
                    pageGoTr.localPosition = new Vector3(pageGoTr.localPosition.x, lastPageGo.transform.localPosition.y - this.pageSize.y);
                    this.pageList.Add(pageGo);
                    pageGo.name = "page" + (this.curPageIndex + this.showPageCount);

                    this.pageItemList.RemoveAt(i);
                    this.pageItemList.Add(itemList);

                    this.curPageIndex++;
                    if (this.curPageIndex == this._pageCount - this.showPageCount)
                        this.setPageItemActive(itemList);
                    break;
                }
                else if (pagePos.y < this.bottom && this.curPageIndex > 0)
                {
                    //向下循环
                    GameObject firstPageGo = this.pageList[0];
                    this.pageList.RemoveAt(i);
                    pageGoTr.localPosition = new Vector3(pageGoTr.localPosition.x,
                                                         firstPageGo.transform.localPosition.y + this.pageSize.y);
                    this.pageList.Insert(0, pageGo);
                    pageGo.name = "page" + (this.curPageIndex + this.showPageCount);

                    this.pageItemList.RemoveAt(i);
                    this.pageItemList.Insert(0, itemList);
                    if (this.curPageIndex == this._pageCount - this.showPageCount)
                        this.setPageItemActive(itemList, false);
                    this.curPageIndex--;
                    break;
                }
            }
        }
    }

    /// <summary>
    /// 执行item回调
    /// </summary>
    /// <param name="isReload"></param>
    private void reloadItem(bool isReload = false)
    {
        if (this.pageList == null || this.pageList.Count == 0) return;
        int pageIndex = 0;
        for (int i = this.curPageIndex; i < this.curPageIndex + this.showPageCount; ++i)
        {
            if (this.pageItemList[pageIndex] != null)
            {
                List<GameObject> itemList = this.pageItemList[pageIndex];
                int count = itemList.Count;
                for (int j = 0; j < count; ++j)
                {
                    int itemIndex = i * this.cellsMaxCountInPage + j;
                    //多余的item不做回调
                    if (itemIndex <= this._cellCount - 1)
                    {
                        GameObject item = itemList[j];
                        if (this.m_updateItem != null)
                            this.m_updateItem.Invoke(item, itemIndex, i, isReload);
                    }
                }
                pageIndex++;
            }
        }
    }

    /// <summary>
    /// 设置页数内的item显示或者隐藏
    /// </summary>
    /// <param name="pageItemList">一页里的item列表</param>
    /// <param name="hide">是否隐藏</param>
    private void setPageItemActive(List<GameObject> pageItemList, bool hide = true)
    {
        if (this.lastPageItemCount == 0) return;
        int count = pageItemList.Count;
        for (int i = 0; i < count; ++i)
        {
            if (hide)
            {
                if (i > this.lastPageItemCount - 1)
                    pageItemList[i].SetActive(false);
                else
                    pageItemList[i].SetActive(true);
            }
            else
            {
                pageItemList[i].SetActive(true);
            }
        }
    }

    /// <summary>
    /// 更新一页内item是否可以显示
    /// </summary>
    private void updatePageItemActive()
    {
        if (this.pageList == null) return;
        if (this.pageList.Count == 0) return;
        for (int i = 0; i < this.showPageCount; ++i)
        {
            List<GameObject> itemList = this.pageItemList[i];
            for (int j = 0; j < this.cellsMaxCountInPage; ++j)
            {
                GameObject item = itemList[j];
                if (this._pageCount <= this.showPageCount) //排数在一屏以内
                {
                    if (i < this.showPageCount - 1) //前几排全部显示
                    {
                        item.SetActive(true);
                    }
                    else
                    {
                        if (j <= this.lastPageItemCount - 1)  //最后一排判断可显示的item
                            item.SetActive(true);
                        else
                            item.SetActive(false);
                    }
                }
                else //排数超过一屏
                {
                    //判断最后一排是否在显示范围内
                    if (this.curPageIndex < this._pageCount - this.showPageCount)
                    {
                        item.SetActive(true);
                    }
                    else
                    {
                        if (i < this.showPageCount - 1) //前几排全部显示
                        {
                            item.SetActive(true);
                        }
                        else
                        {
                            if (j <= this.lastPageItemCount - 1)  //最后一排判断可显示的item
                                item.SetActive(true);
                            else
                                item.SetActive(false);
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// 获取第一页的位置
    /// </summary>
    private Vector2 getFirstPagePos()
    {
        //保存上一次第一个item的位置
        if (this.pageList == null || 
            this.pageList.Count == 0) return new Vector2();
        GameObject pageGo = this.pageList[0];
        return pageGo.transform.localPosition;
    }

    /// <summary>
    /// page布局
    /// </summary>
    /// <returns></returns>
    private void layoutPage()
    {
        if (this.pageList == null) return;
        int count = this.pageList.Count;
        for (int i = 0; i < count; ++i)
        {
            GameObject pageGo = this.pageList[i];
            if (!this.isHorizontal)
                pageGo.transform.localPosition = new Vector3(0, this.firstPagePos.y - this.pageSize.y * i);
            else
                pageGo.transform.localPosition = new Vector3(this.firstPagePos.x + this.pageSize.x * i, 0);
        }
    }

    /// <summary>
    /// 删除多余的页数
    /// </summary>
    private void removeOverPage()
    {
        if (this.pageList == null || 
            this.pageList.Count == 0) return;
        if (this._pageCount >= this.showPageCount) return;
        for (int i = this.showPageCount - 1; i >= this._pageCount; --i)
        {
            List<GameObject> itemList = this.pageItemList[i];
            int count = itemList.Count;
            for (int j = count - 1; j >= 0; --j)
            {
                GameObject item = itemList[j];
                GameObject.Destroy(item);
                itemList.RemoveAt(j);
            }
            this.pageItemList.RemoveAt(i);
            GameObject.Destroy(this.pageList[i]);
            this.pageList.RemoveAt(i);
        }
    }

    /// <summary>
    /// 更新边界
    /// </summary>
    /// <returns></returns>
    void updateBorder()
    {
        //上下
        this.top = this.pageSize.y;
        this.bottom = -this.pageSize.y * (this.showPageCount - 1);
        //左右
        this.left = -this.pageSize.x;
        this.right = this.pageSize.x * (this.showPageCount - 1);
    }

    /// <summary>
    /// 根据页数返回页数容器
    /// </summary>
    /// <param name="page">页数索引</param>
    /// <returns>页数显示容器</returns>
    private GameObject getPageGoByPageIndex(int page)
    {
        if (this.pageList == null ||
            this.pageList.Count == 0 ||
            page > this.pageList.Count - 1) return null;
        return this.pageList[page];
    }

    /// <summary>
    /// 根据页数滚动到相应位置
    /// </summary>
    /// <param name="page">页数索引</param>
    public void rollPosByPage(int page)
    {
        if (this.pageList == null || 
            this.pageList.Count == 0) return;
        this.sr.StopMovement();
        if (page < 0) page = 0;
        if (page > this._pageCount - 1) page = this._pageCount - 1;
        this.curPageIndex = page;
        //计算出第一个索引是多少， 因为第一个curLineIndex不一定是targetLineIndex 
        if (this.curPageIndex + this.showPageCount > this._pageCount)
            this.curPageIndex -= page + this._pageCount - this.showPageCount;
        Vector3 contentPos = this.contentRectTf.localPosition;
        if (!this.isHorizontal)
        {
            this.firstPagePos.y = -this.pageSize.y * this.curPageIndex; //算出移动的距离
            contentPos.y = this.pageSize.y * page;
        }
        else
        {
            this.firstPagePos.x = this.pageSize.x * this.curPageIndex; //算出移动的距离
            contentPos.x = -this.pageSize.x * page;
        }
        this.contentRectTf.localPosition = contentPos;
        this.reloadItem(true);
        this.layoutPage();
        this.updatePageItemActive();
        this.fixContentPos();
    }

    /// <summary>
    /// 根据item索引跳转到页数
    /// </summary>
    /// <param name="itemIndex">item的索引</param>
    public void rollPosByPageByIndex(int itemIndex)
    {
        if (itemIndex < 0) itemIndex = 0;
        if (itemIndex > this._cellCount - 1) itemIndex = this._cellCount - 1;
        int page = Mathf.CeilToInt((itemIndex + 1) / this.cellsMaxCountInPage);
        this.rollPosByPage(page);
    }

	// Update is called once per frame
	void Update () 
    {
        this.updatePage();
        //重新调用item回调
        this.reloadItem();
	}
}

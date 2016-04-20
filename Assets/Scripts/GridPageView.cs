using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;
/// <summary>
/// 格子翻页组件
/// TODO:
/// [找到点击释放的事件]
/// [onDragEnd后翻页状态 滚动页面]
/// 隐藏和显示多余的item
/// 添加和删除item或者页数
/// 跳转到某一页
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
    private int pageCount;
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
    private int cellCount;
    //行数(决定list的高度)
    private int rows;
    //列数(决定list的宽度)
    private int columns;
    //当前页数
    private int curPageIndex;
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
        this.cellCount = cellCount;

        this.isHorizontal = isHorizontal;
        this.curPageIndex = 0;
        //计算页数
        if (this.cellsMaxCountInPage % cellCount == 0)
            this.pageCount = cellCount / this.cellsMaxCountInPage;
        else
            this.pageCount = cellCount / this.cellsMaxCountInPage + 1;
        this.gapH = gapH;
        this.gapV = gapV;
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
        this.updateContentSize();

        //获取最新边界
        this.updateBorder();
        if (this.pageCount < this.showPageCount)
        {
            this.showPageCount = this.pageCount;
            this.createPageItem(this.itemPrefab, this.pageCount);
        }
        else
        {
            this.createPageItem(this.itemPrefab, this.showPageCount);
        }
    }

    /// <summary>
    /// 创建一个页的内容
    /// </summary>
    /// <param name="prefab">预制件</param>
    /// <param name="showPageCount">可显示的页数</param>
    private void createPageItem(GameObject prefab, int showPageCount)
    {
        if (this.pageList == null) 
            this.pageList = new List<GameObject>();

        if (this.pageItemList == null)
            this.pageItemList = new List<List<GameObject>>();

        for (int i = 0; i < showPageCount; ++i)
        {
            GameObject pageContainer = new GameObject();
            pageContainer.AddComponent<RectTransform>();
            pageContainer.AddComponent<CanvasRenderer>();
            pageContainer.transform.SetParent(this.content.gameObject.transform);
            pageContainer.GetComponent<RectTransform>().pivot = new Vector2(0, 1);
            pageContainer.GetComponent<RectTransform>().sizeDelta = this.pageSize;
            pageContainer.transform.localScale = new Vector3(1, 1, 1);

            List<GameObject> list = new List<GameObject>();
            this.pageItemList.Add(list);

            //设置每一页的位置
            Vector2 pagePos;
            if (this.isHorizontal)
                pagePos = new Vector2(this.pageSize.x * i, 0);
            else
                pagePos = new Vector2(0, -this.pageSize.y * i);
            pageContainer.transform.localPosition = pagePos;
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
                rows++;
                if (rows == this.rows)
                {
                    rows = 0;
                    columns++;
                }
            }
        }
    }

    /// <summary>
    /// 更新滚动内容的大小
    /// </summary>
    private void updateContentSize()
    {
        Vector2 size;
        if (this.isHorizontal)
            size = new Vector2(this.pageSize.x * this.pageCount,
                               this.pageSize.y);
        else
            size = new Vector2(this.pageSize.x,
                               this.pageSize.y * this.pageCount);
        this.contentRectTf.sizeDelta = size;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        //return;
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
                    this.content.transform.DOLocalMoveX(beforeDragPagePos.x - this.pageSize.x - .01f, .4f); //翻页
                else
                    this.content.transform.DOLocalMoveX(beforeDragPagePos.x, .4f); //回滚
            }
            else if (afterDragPagePos.x > beforeDragPagePos.x)
            {
                //往右拖动
                if (afterDragPagePos.x > beforeDragPagePos.x + this.pageSize.x * .5f)
                    this.content.transform.DOLocalMoveX(beforeDragPagePos.x + this.pageSize.x + .01f, .4f); //翻页
                else
                    this.content.transform.DOLocalMoveX(beforeDragPagePos.x, .4f); //回滚
            }
        }
        else
        {
            //纵向
            if (afterDragPagePos.y > beforeDragPagePos.y)
            {
                //向上
                if (afterDragPagePos.y > beforeDragPagePos.y + this.pageSize.y * .5f)
                    this.content.transform.DOLocalMoveY(beforeDragPagePos.y + this.pageSize.y + .01f, .4f); //翻页
                else
                    this.content.transform.DOLocalMoveY(beforeDragPagePos.y, .4f); //回滚
            }
            else if (afterDragPagePos.y < beforeDragPagePos.y)
            {
                //向下
                if (afterDragPagePos.y < beforeDragPagePos.y - this.pageSize.y * .5f)
                    this.content.transform.DOLocalMoveY(beforeDragPagePos.y - this.pageSize.y - .01f, .4f); //翻页
                else
                    this.content.transform.DOLocalMoveY(beforeDragPagePos.y, .4f); //回滚
            }
        }
    }

    //更新page
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
                if (pagePos.x < this.left && this.curPageIndex < this.pageCount - this.showPageCount)
                {
                    //向左循环
                    GameObject lastPageGo = this.pageList[this.pageList.Count - 1];
                    this.pageList.RemoveAt(i);
                    pageGoTr.localPosition = new Vector3(lastPageGo.transform.localPosition.x + this.pageSize.x,
                                                         pageGoTr.localPosition.y);
                    this.pageList.Add(pageGo);

                    this.pageItemList.RemoveAt(i);
                    this.pageItemList.Add(itemList);

                    this.curPageIndex++;
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

                    this.pageItemList.RemoveAt(i);
                    this.pageItemList.Insert(0, itemList);
                    this.curPageIndex--;
                    break;
                }
            }
            else
            {
                if (pagePos.y > this.top && this.curPageIndex < this.pageCount - this.showPageCount)
                {
                    //向上循环
                    GameObject lastPageGo = this.pageList[this.pageList.Count - 1];
                    this.pageList.RemoveAt(i);
                    pageGoTr.localPosition = new Vector3(pageGoTr.localPosition.x, lastPageGo.transform.localPosition.y - this.pageSize.y);
                    this.pageList.Add(pageGo);

                    this.pageItemList.RemoveAt(i);
                    this.pageItemList.Add(itemList);

                    this.curPageIndex++;
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

                    this.pageItemList.RemoveAt(i);
                    this.pageItemList.Insert(0, itemList);

                    this.curPageIndex--;
                    break;
                }
            }
        }
    }

    /// <summary>
    /// 执行item回调
    /// </summary>
    private void reloadItem(bool isReload = false)
    {
        if (this.pageList == null || this.pageList.Count == 0) return;
        int index = 0;
        for (int i = this.curPageIndex; i < this.curPageIndex + this.showPageCount; ++i)
        {
            if (this.pageItemList[index] != null)
            {
                List<GameObject> itemList = this.pageItemList[index];
                int count = itemList.Count;
                for (int j = 0; j < count; ++j)
                {
                    GameObject item = itemList[j];
                    if (this.m_updateItem != null)
                        this.m_updateItem.Invoke(item, j, i, isReload);
                }
                index++;
            }
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
    /// <param name="index">页数</param>
    /// <returns></returns>
    private GameObject getPageGoByPageIndex(int index)
    {
        if (this.pageList == null ||
            this.pageList.Count == 0 ||
            index > this.pageList.Count - 1) return null;
        return this.pageList[index];
    }

	// Update is called once per frame
	void Update () 
    {
        this.updatePage();
        //重新调用item回调
        this.reloadItem();
	}
}

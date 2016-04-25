using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class Test : MonoBehaviour
{
    public Button addBtn;
    public Button rollBtn;
    public GridPageView gpView;
    public Text pageTxt;
    // Use this for initialization
    void Start()
    {
        this.addBtn.onClick.AddListener(addBtnClickHandler);
        this.rollBtn.onClick.AddListener(rollBtnClickHandler);
        this.gpView.init(4, 4, 200, true, 5, 5, updateItem);
    }

    private void rollBtnClickHandler()
    {
        //int page = Random.Range(0, this.gpView.pageCount - 1);
        //print("page " + page);
        //this.gpView.rollPosByPage(page);
        int itemIndex = Random.Range(0, this.gpView.cellCount - 1);
        print("itemIndex " + itemIndex);
        this.gpView.rollPosByPageByIndex(itemIndex);
    }

    private void addBtnClickHandler()
    {
        int count = Random.Range(40, 80);
        count = 64;
        print("count" + count);
        this.gpView.reloadData(count);
    }

    private void updateItem(GameObject item, int index, int pageIndex, bool isReload)
    {
        item.GetComponent<GridItem>().txt.text = pageIndex + "_" + index;
    }

    // Update is called once per frame
    void Update()
    {
        this.pageTxt.text = "第" + this.gpView.pageIndex.ToString() + "页";
    }
}

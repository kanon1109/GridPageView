using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class Test : MonoBehaviour
{
    public Button addBtn;
    public GridPageView gpView;
    // Use this for initialization
    void Start()
    {
        this.addBtn.onClick.AddListener(addBtnClickHandler);
        this.gpView.init(4, 4, 79, true, 5, 5, updateItem);
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

    }
}

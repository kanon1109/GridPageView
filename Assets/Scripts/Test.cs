using UnityEngine;
using System.Collections;

public class Test : MonoBehaviour
{
    public GridPageView gpView;
    // Use this for initialization
    void Start()
    {
        this.gpView.init(4, 3, 40, true, 5, 5, updateItem);
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

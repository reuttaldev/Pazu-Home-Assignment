using UnityEngine;

public class HairManager : MonoBehaviour
{
    [SerializeField] int cardCount = 20;
    [SerializeField] float maxHeight = 1.7f;    // full card length in world units
    [SerializeField] float defaultHeight = 1f;    // full card length in world units

    [SerializeField] float minHeight = 0.05f;   // minimum after cut — never zero
    [SerializeField] float width = 1f;

    [SerializeField] float arcRadiusX = 0.6f;
    [SerializeField] float arcRadiusY = 0.4f;
    [SerializeField] int layerCount = 5;
    [SerializeField] float layerSpacing = 0.05f;
    float arcDeg = 85f;

    [SerializeField] GameObject[] cardPrefabs;

    HairCard[] _cards;
    float noiseVal = 0.08f;


    void Awake()
    {
        _cards = new HairCard[cardCount];
        SpawnCards();
    }

#region SPAN CARDS  
    // generates cardCount hair cards along a semicircular arc 
    private float GetAngleOnArc(int localIndex, int cardsInLayer)
    {
        // each layer independently spans the full arc
        float t = cardsInLayer > 1 ? (float)localIndex / (cardsInLayer - 1) : 0.5f;
        float angle = Mathf.Lerp(-arcDeg, arcDeg, t);
        angle += Random.Range(-3f, 3f); // break symmetry
        return angle;
    }

    private Vector2 GetPosOnArc(float angle, float layerOffset)
    {
        float rad = angle * Mathf.Deg2Rad; // turn to rad
        // the direction of our angle in a unit circle
        float noise = Random.Range(-noiseVal, noiseVal);
        float x = Mathf.Sin(rad) * (arcRadiusX + noise + layerOffset);
        float y = Mathf.Cos(rad) * (arcRadiusY + noise + layerOffset);
        Vector2 headCenter = transform.position;
        return headCenter + new Vector2(x, y);
    }

    private void CreateCardObject(Vector2 pos, float angle, int count,Transform parent,int layer)
    {
        GameObject prefab = cardPrefabs[Random.Range(0, cardPrefabs.Length)];

        GameObject go = Instantiate( prefab,
                new Vector3(pos.x, pos.y, 0f),
                Quaternion.Euler(0f, 0f, -angle),
                parent);
        go.name = "Card_" + count;
        // set a random sorting orde so some hairs are behind the ears and some are above it
        go.GetComponent<SpriteRenderer>().sortingOrder = layer + Random.Range(-1, 1);
        
        // add randomness in appearances
        float scaleVar = Random.Range(0.95f, 1.05f);
        float heightFrac = Random.Range(0.85f, 1.0f);
        float xScale = width * scaleVar * (0.9f + 0.1f * heightFrac);
        float yScale = defaultHeight * heightFrac * scaleVar;
        go.transform.localScale = new Vector3(xScale, yScale, 4f);
        SetupCard(go,count);
    }
    private void SetupCard(GameObject go, int count)
    {
        HairCard card = go.AddComponent<HairCard>();
        card.Init();
        card.RestPosition = go.transform.position;
        card.BaseRotation = go.transform.rotation;
        card.HeightFraction = go.transform.localScale.y / defaultHeight;
        _cards[count] = card;

    }
    private Transform CreateLayerParent(int layerIndex)
    {
        GameObject layerGO = new GameObject("Layer_" + layerIndex);
        layerGO.transform.SetParent(transform);
        return layerGO.transform;
    }
    void SpawnCards()
    {
        int counter = 0;
        int cardsPerLayer = Mathf.CeilToInt((float)cardCount / layerCount);
        for (int layer = 0; layer < layerCount; layer++)
        {
            float layerOffset = layerSpacing * -layer;
            Transform layerParent = CreateLayerParent(layer);
            for (int i = 0; i < cardsPerLayer && counter < cardCount; i++, counter++)
            {
                float hairAngle = GetAngleOnArc(i, cardsPerLayer);
                Vector2 hairPos = GetPosOnArc(hairAngle,layerOffset);
                CreateCardObject(hairPos,hairAngle,counter,layerParent,layer);
            }

        }
    }
}
#endregion
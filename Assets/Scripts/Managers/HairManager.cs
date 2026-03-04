using UnityEngine;

public class HairManager : MonoBehaviour
{
    [Header("Hair settings")]
    [SerializeField] int cardCount = 20;
    [SerializeField] float maxLength = 1.7f;
    [SerializeField] float defaultLength = 1f;
    [SerializeField] float minLength = 0.05f;
    [SerializeField] float width = 1f;

    [SerializeField] float arcRadiusX = 0.6f;
    [SerializeField] float arcRadiusY = 0.4f;
    [SerializeField] int layerCount = 5;
    [SerializeField] float layerSpacing = 0.05f;
    float arcDeg = 85f;

    [SerializeField] GameObject[] cardPrefabs;

    [Header("Scissors")]
    [SerializeField] Scissors scissors;
    [SerializeField] float bladeRadius = 0.2f;

    [Header("Hair Extension")]
    [SerializeField] HairExtension hairExtension;
    [SerializeField] float growRadius = 0.6f;
    [SerializeField] float growRate = 0.5f;
    [Header("Dryer")]
    [SerializeField] HairDryer hairDryer;
    [SerializeField] float windStrength = 30f;
    [SerializeField] float windRange = 1.5f;
    [SerializeField] float windWidth = 0.5f;

    const float unitWorldLen = 1.6f; // world units per unit of card localScale.y (hair.png: 169px @ 100 PPU)

    HairCard[] cards;
    float noiseVal = 0.08f;
    float bladeRadiusSq;
    float growRadiusSq;
    float wRangeSq;
    float wWidthSq;
    void Awake()
    {
        cards = new HairCard[cardCount];
        bladeRadiusSq = bladeRadius * bladeRadius;
        growRadiusSq  = growRadius  * growRadius;
        wRangeSq = windRange * windRange;
        wWidthSq = windWidth * windWidth;
        SpawnCards();
    }
public void ApplyWind(Vector2 toolPos, Vector2 windDir)
{
    windDir = windDir.normalized;
    float targetZ = Mathf.Atan2(-windDir.x, windDir.y) * Mathf.Rad2Deg;
    for (int i = 0; i < cards.Length; i++)
    {
        HairCard card = cards[i];
        if (!IsToolInRadius(toolPos, card, wRangeSq, wWidthSq, out float normSum))
            continue;

        float currentZ  = card.transform.eulerAngles.z;
        float zRotation = Mathf.LerpAngle(currentZ, targetZ, Time.deltaTime * windStrength * (1f - normSum));
        card.transform.rotation = Quaternion.Euler(0f, 0f, zRotation);
    }
}
    public void CutHair(Vector2 toolPos)
    {
        for (int i = 0; i < cards.Length; i++)
        {
            HairCard card = cards[i];
            if(card.currentLength == minLength) // already the shortest possible, can't cut
                continue;
            // is the hair close enough to this hair card?
            if (!IsToolInRadius(toolPos, card, maxLength, bladeRadiusSq, out _))
                continue;

            // project onto card's local up axis to find where along the card to cut
            float projectedLength = Vector2.Dot(toolPos - (Vector2)card.transform.position, (Vector2)card.transform.up);
            SetHairLength(card, Mathf.Max(projectedLength, minLength));
        }
    }
    public void GrowHair(Vector2 toolPos)
    {
        for (int i = 0; i < cards.Length; i++)
        {
            HairCard card = cards[i];
            if(card.currentLength == maxLength) // already the longest possible
                continue;
            // is the hair close enough to this hair card?
            if (!IsToolInRadius(toolPos, card, maxLength, growRadiusSq, out _))
                continue;
            SetHairLength(card, Mathf.Min(card.currentLength + growRate * Time.deltaTime, maxLength));
        }
    }
    void SetHairLength(HairCard card, float newLen)
    {
        card.currentLength = newLen;
        Vector3 s = card.transform.localScale;
        card.transform.localScale = new Vector3(s.x, newLen / unitWorldLen, s.z);
    }

#region HAIR DISTANCE FROM TOOL
    // true only when toolPos is within radiusSq of the card segment (root → tip)
    // rejects entirely if the projection falls outside [0, currentLength]
    bool IsToolInRadius(Vector2 toolPos, HairCard card, float alongRadiusSq, float perpRadiusSq, out float normSum)
    {
        normSum = 0f;
        Vector2 root = (Vector2)card.transform.position;
        Vector2 hairDir    = (Vector2)card.transform.up; // direction of the hair w.r.t the angle it is at
        Vector2 rootToTool = toolPos - root;
        // the projection is the parallel of toTool (line from root of hair to tool) along the hair
        // it tells us how far along the hair direction is the tool
        float alongHairProj = Vector2.Dot(rootToTool, hairDir);
        // if the tool is below the root or above the tip
        if (alongHairProj < 0f || alongHairProj > card.currentLength)
            return false;
        // calculate the distance from the hair:
        // it is the vector starting at the tool and ending perpendicular to the projection along the hair
        // you find it by removing the along the hair part
        Vector2 perp = rootToTool - hairDir * alongHairProj;
        normSum = alongHairProj * alongHairProj / alongRadiusSq + perp.sqrMagnitude / perpRadiusSq;
        return normSum <= 1f;
    }

#endregion
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

    private void CreateCardObject(Vector2 pos, float angle, int count, Transform parent, int layer)
    {
        GameObject prefab = cardPrefabs[Random.Range(0, cardPrefabs.Length)];
        GameObject go = Instantiate(prefab,
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
        float yScale = defaultLength * heightFrac * scaleVar;
        go.transform.localScale = new Vector3(xScale, yScale, 4f);
        SetupCard(go, count);
    }

    private void SetupCard(GameObject go, int count)
    {
        HairCard card = go.AddComponent<HairCard>();
        card.Init();
        card.currentLength = unitWorldLen * go.transform.localScale.y;
        card.RestPosition  = go.transform.position;
        card.BaseRotation  = go.transform.rotation;
        card.PhaseOffset   = Random.Range(0f, 2f * Mathf.PI);
        cards[count] = card;
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
                Vector2 hairPos = GetPosOnArc(hairAngle, layerOffset);
                CreateCardObject(hairPos, hairAngle, counter, layerParent, layer);
            }
        }
    }
#endregion
#region DEBUG
    void OnDrawGizmos()
    {
        if (scissors != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere((Vector2)scissors.transform.position + scissors.Offset, bladeRadius);
        }
        if (hairExtension != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere((Vector2)hairExtension.transform.position + hairExtension.Offset, growRadius);
        }
        if (hairDryer != null)
        {
            Gizmos.color = Color.magenta;
            Vector3 pos   = hairDryer.transform.position;
            Vector3 windDir = hairDryer.transform.right;
            Vector3 right   = hairDryer.transform.up;
            Vector3 tip   = pos + windDir * windRange;
            // four corners of the rectangular beam
            Vector3 nearTop    = pos + right *  windWidth;
            Vector3 nearBottom = pos + right * -windWidth;
            Vector3 farTop     = tip + right *  windWidth;
            Vector3 farBottom  = tip + right * -windWidth;
            Gizmos.DrawLine(nearTop,    farTop);
            Gizmos.DrawLine(nearBottom, farBottom);
            Gizmos.DrawLine(nearTop,    nearBottom);
            Gizmos.DrawLine(farTop,     farBottom);
        }
    }
#endregion
}

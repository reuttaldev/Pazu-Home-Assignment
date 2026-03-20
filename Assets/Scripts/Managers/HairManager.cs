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
    [SerializeField] float bladeLength = 0.4f;
    [SerializeField] float bladeRadius = 0.2f;
    [SerializeField] Vector2 scissorsOffset;

    [Header("Hair Extension")]
    [SerializeField] HairExtension hairExtension;
    [SerializeField] float growLength = 0.8f;
    [SerializeField] float growRadius = 0.6f;
    [SerializeField] float growRate = 0.5f;
    [SerializeField] Vector2 extensionOffset;

    [Header("Dryer")]
    [SerializeField] HairDryer hairDryer;
    [SerializeField] float windStrength = 30f;
    [SerializeField] float windRange = 1.5f;
    [SerializeField] float windWidth = 0.5f;
    [SerializeField] float windFalloffPower = 1f;
    [SerializeField] float windSpread = 60f;
    [SerializeField] float windSpreadCurve = 2f;
    [SerializeField] float windSpreadWidth = 0.5f;
    [SerializeField] float blastRadius = 0.3f;
    [SerializeField] float dryerAnimTime = 0f,dryerAnimDuration=0.1f;
    [SerializeField] Vector2 dryerOffset;

    const float unitWorldLen = 1.6f; // world units per unit of card localScale.y (hair.png: 169px @ 100 PPU)

    HairCard[] cards;
    float noiseVal = 0.08f;
    float bladeLengthSq;
    float bladeRadiusSq;
    float growLengthSq;
    float growRadiusSq;
    float wRangeSq;
    float wWidthSq;
    float blastRadiusSq;
    void Awake()
    {
        cards = new HairCard[cardCount];
        bladeLengthSq = bladeLength * bladeLength;
        bladeRadiusSq = bladeRadius * bladeRadius;
        growLengthSq  = growLength  * growLength;
        growRadiusSq  = growRadius  * growRadius;
        wRangeSq      = windRange   * windRange;
        wWidthSq      = windWidth   * windWidth;
        blastRadiusSq = blastRadius * blastRadius;
        SpawnCards();
    }
public void ApplyWind(Vector2 toolPos)
{
    toolPos += (Vector2)(hairDryer.transform.rotation * (Vector3)dryerOffset);
    Vector2 windDir  = ((Vector2)hairDryer.transform.right).normalized;
    Vector2 windPerp = new Vector2(-windDir.y, windDir.x);
    float baseTargetZ = Mathf.Atan2(-windDir.x, windDir.y) * Mathf.Rad2Deg;

    // advance animation timer once per call, not once per card
    dryerAnimTime += Time.deltaTime;
    bool flipThisFrame = dryerAnimTime >= dryerAnimDuration;
    if (flipThisFrame)
        dryerAnimTime -= dryerAnimDuration;

    for (int i = 0; i < cards.Length; i++)
    {
        HairCard card = cards[i];
        Vector2 toCard = (Vector2)card.transform.position - toolPos;
        float distSq   = toCard.sqrMagnitude;

        // check cone (directed airflow) and blast (strong close-range nozzle effect)
        bool inCone  = IsToolInRadius(toolPos, card, wRangeSq, wWidthSq, out float normSum, windDir);
        bool inBlast = distSq < blastRadiusSq;
        if (!inCone && !inBlast) continue;

        // cone falloff: polynomial attenuation from nozzle outward
        float coneFalloff  = inCone  ? Mathf.Pow(1f - normSum, windFalloffPower) : 0f;
        // blast falloff: linear radial dropoff very close to the nozzle
        float blastFalloff = inBlast ? 1f - Mathf.Sqrt(distSq) / blastRadius     : 0f;
        float falloff = Mathf.Max(coneFalloff, blastFalloff);

        // signed lateral distance from wind axis → fan-out direction
        float lateralSigned = Vector2.Dot(toCard, windPerp);
        float lateralNorm   = Mathf.Clamp01(Mathf.Abs(lateralSigned) / windSpreadWidth); // 0 at center, 1 at edge

        // convex curve: small spread near center, large at edges
        float spreadAngle = Mathf.Sign(lateralSigned)
                          * Mathf.Pow(lateralNorm, windSpreadCurve)
                          * windSpread;

        float targetZ  = baseTargetZ + spreadAngle;
        float currentZ = card.transform.eulerAngles.z;
        float zRotation = Mathf.LerpAngle(currentZ, targetZ, Time.deltaTime * windStrength * falloff);
        card.transform.rotation = Quaternion.Euler(0f, 0f, zRotation);

        if (flipThisFrame)
        {
            Vector3 s = card.transform.localScale;
            card.transform.localScale = new Vector3(-s.x, s.y, s.z);
        }
    }
}
    public void CutHair(Vector2 toolPos)
    {
        toolPos += (Vector2)scissors.transform.TransformVector(scissorsOffset);
        for (int i = 0; i < cards.Length; i++)
        {
            HairCard card = cards[i];
            if(card.currentLength == minLength) // already the shortest possible, can't cut
                continue;
            // is the hair close enough to this hair card?
            if (!IsToolInRadius(toolPos, card, bladeLengthSq, bladeRadiusSq, out _, (Vector2)scissors.transform.right))
                continue;

            // project onto card's local up axis to find where along the card to cut
            Vector2 root = (Vector2)card.transform.position;
            float projectedLength = Vector2.Dot(toolPos - root, (Vector2)card.transform.up);
            SetHairLength(card, Mathf.Max(projectedLength, minLength));
        }
    }
    public void GrowHair(Vector2 toolPos)
    {
        toolPos += (Vector2)hairExtension.transform.TransformVector(extensionOffset);
        for (int i = 0; i < cards.Length; i++)
        {
            HairCard card = cards[i];
            if(card.currentLength == maxLength) // already the longest possible
                continue;
            // is the hair close enough to this hair card?
            if (!IsToolInRadius(toolPos, card, growLengthSq, growRadiusSq, out _, (Vector2)hairExtension.transform.right))
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
bool IsToolInRadius(Vector2 toolPos,HairCard card, float alongRadiusSq, float perpRadiusSq,out float normSum,Vector2 toolDir)
{
    normSum = 0f;
    toolDir.Normalize();
    Vector2 root = (Vector2)card.transform.position;
    Vector2 hairDir = ((Vector2)card.transform.up).normalized;
    Vector2 rootToTool = toolPos - root;
    // the projection is the parallel of toTool (line from root of hair to tool) along the hair
    // it tells us how far along the hair direction is the tool
    float alongHair = Vector2.Dot(rootToTool, hairDir);
    // clamp root to tip
    alongHair = Mathf.Clamp(alongHair, 0f, card.currentLength);
    // closest point on the hair to the tool
    Vector2 closestPoint = root + hairDir * alongHair;
    // measure from tool → that point
    Vector2 toolToPoint = closestPoint - toolPos;
    float alongTool = Vector2.Dot(toolToPoint, toolDir);

    // reject if the closest point is behind the tool or beyond its range
    if (alongTool < 0f || alongTool * alongTool > alongRadiusSq)
        return false;
    // calculate the distance from the hair:
    // it is the vector starting at the tool and ending perpendicular to the projection along the hair
    // you find it by removing the along the hair part
    Vector2 perp = toolToPoint - toolDir * alongTool;
    normSum = (alongTool * alongTool) / alongRadiusSq +(perp.sqrMagnitude) / perpRadiusSq;
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
        card.currentLength = unitWorldLen * go.transform.localScale.y;
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
            Vector2 scissorsPos = (Vector2)scissors.transform.position + (Vector2)scissors.transform.TransformVector(scissorsOffset);
            DrawEllipseGizmo(scissorsPos, scissors.transform.right, bladeLength, bladeRadius);
        }
        if (hairExtension != null)
        {
            Gizmos.color = Color.green;
            Vector2 extensionPos = (Vector2)hairExtension.transform.position + (Vector2)hairExtension.transform.TransformVector(extensionOffset);
            DrawEllipseGizmo(extensionPos, hairExtension.transform.right, growLength, growRadius);
        }
        if (hairDryer != null)
        {
            Gizmos.color = Color.magenta;
            Vector2 pos = (Vector2)hairDryer.transform.position + (Vector2)hairDryer.transform.TransformVector(dryerOffset);
            // blast radius: close-range nozzle effect (circle)
            DrawEllipseGizmo(pos, hairDryer.transform.right, blastRadius, blastRadius);
            // wind cone: matches the normSum ellipse used in IsToolInRadius
            DrawEllipseGizmo(pos, hairDryer.transform.right, windRange, windWidth);
        }
    }
    void DrawEllipseGizmo(Vector2 center, Vector2 forward, float a, float b, int segments = 32)
    {
        Vector2 right = new(-forward.y, forward.x);
        Vector3 prev = center + forward * a;
        for (int i = 1; i <= segments; i++)
        {
            float angle = i * Mathf.PI * 2f / segments;
            Vector3 next = (Vector3)(center + forward * (Mathf.Cos(angle) * a) + right * (Mathf.Sin(angle) * b));
            Gizmos.DrawLine(prev, next);
            prev = next;
        }
    }
#endregion
}

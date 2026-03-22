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
    [SerializeField] float windForwardFalloffPower = 1f;
    [SerializeField] float windLateralFalloffPower = 1f;
    [SerializeField] float windMaxSpread = 45f;
    [SerializeField] Vector2 dryerOffset;
    [SerializeField] float dryerAnimDuration = 0.5f;

    const float unitWorldLen = 1.6f; // world units per unit of card localScale.y (hair.png: 169px @ 100 PPU)

    HairCard[] cards;
    float noiseVal = 0.08f;
    float bladeLengthSq;
    float bladeRadiusSq;
    float growLengthSq;
    float growRadiusSq;
    float wRangeSq;
    float wWidthSq;
    float dryerAnimTime;
    void Awake()
    {
        cards = new HairCard[cardCount];
        bladeLengthSq = bladeLength * bladeLength;
        bladeRadiusSq = bladeRadius * bladeRadius;
        growLengthSq  = growLength  * growLength;
        growRadiusSq  = growRadius  * growRadius;
        wRangeSq = windRange * windRange;
        wWidthSq = windWidth * windWidth;
        SpawnCards();
    }
    public void ApplyWind(Vector2 toolPos)
    {
        toolPos += (Vector2)hairDryer.transform.TransformVector(dryerOffset);
        Vector2 windDir = ((Vector2)hairDryer.transform.right).normalized;
        Vector2 windPerp = new(-windDir.y, windDir.x);
        float baseTargetZ = Mathf.Atan2(-windDir.x, windDir.y) * Mathf.Rad2Deg;
        
        dryerAnimTime += Time.deltaTime;
        bool flipThisFrame = dryerAnimTime >= dryerAnimDuration;
        if (flipThisFrame)
            dryerAnimTime -= dryerAnimDuration;

        for (int i = 0; i < cards.Length; i++)
        {
            HairCard card = cards[i];
           IsToolInRadius(toolPos, card, wRangeSq, wWidthSq, out float forwardNormSq, out float lateralNormSq, out Vector2 perp, windDir);
            // lateralNorm: 0 on wind axis, 1 at cone edge — scaled by windSpread to get degrees
            // sign from perp (already computed in IsToolInRadius): tells left vs right of wind axis
            float forwardNorm = Mathf.Sqrt(forwardNormSq);
            float lateralNorm = Mathf.Sqrt(lateralNormSq);
            float sign = Mathf.Sign(Vector2.Dot(perp, windPerp));
            float forwardFalloff = Mathf.Pow(1f - forwardNorm, windForwardFalloffPower);
            float lateralSpread = Mathf.Pow(lateralNorm, windLateralFalloffPower);
            float coneAngle = lateralSpread * windMaxSpread * sign * forwardFalloff;
            float targetZ= baseTargetZ + coneAngle;

            // Debug.Log($"[{card.name}] lateralNorm={lateralNorm:F3} forwardNorm={forwardNorm:F3} lateralSpread={lateralSpread:F3} forwardFalloff={forwardFalloff:F3} coneAngle={coneAngle:F2}");

            float currentZ = card.transform.eulerAngles.z;
            card.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.LerpAngle(currentZ, targetZ, windStrength * Time.deltaTime));

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
            if (!IsToolInRadius(toolPos, card, bladeLengthSq, bladeRadiusSq, out _, out _, out _, (Vector2)scissors.transform.right))
                continue;

            // project onto card's local up axis to find where along the card to cut
            Vector2 root = (Vector2)card.transform.position;
            float projectedLength = Vector2.Dot(toolPos - root, (Vector2)card.transform.up);
            SetHairLength(card, Mathf.Max(projectedLength, minLength));
        }
    }
    public bool GrowHair(Vector2 toolPos)
    {
        toolPos += (Vector2)hairExtension.transform.TransformVector(extensionOffset);
        bool anyGrown = false;
        for (int i = 0; i < cards.Length; i++)
        {
            HairCard card = cards[i];
            if(card.currentLength == maxLength) // already the longest possible
                continue;
            // is the hair close enough to this hair card?
            if (!IsToolInRadius(toolPos, card, growLengthSq, growRadiusSq, out _, out _, out _, (Vector2)hairExtension.transform.right))
                continue;
            SetHairLength(card, Mathf.Min(card.currentLength + growRate * Time.deltaTime, maxLength));
            anyGrown = true;
        }
        return anyGrown;
    }
    void SetHairLength(HairCard card, float newLen)
    {
        card.currentLength = newLen;
        Vector3 s = card.transform.localScale;
        card.transform.localScale = new Vector3(s.x, newLen / unitWorldLen, s.z);
    }

#region HAIR DISTANCE FROM TOOL
    bool IsToolInRadius(Vector2 toolPos, HairCard card, float alongRadiusSq, float perpRadiusSq, out float forwardNormSq, out float lateralNormSq, out Vector2 perp, Vector2 toolDir)
    {
        forwardNormSq = 0f;
        lateralNormSq = 0f;
        perp = Vector2.zero;
        toolDir.Normalize();
        Vector2 root = (Vector2)card.transform.position;
        Vector2 hairDir = ((Vector2)card.transform.up).normalized;
        Vector2 rootToTool = toolPos - root;
        // projection of (tool − root) onto the hair direction: how far along the hair the tool sits
        float alongHair = Vector2.Dot(rootToTool, hairDir);
        // clamp to [root, tip]
        alongHair = Mathf.Clamp(alongHair, 0f, card.currentLength);
        // closest point on the hair segment to the tool
        Vector2 closestPoint = root + hairDir * alongHair;
        // vector from tool to that closest point, decomposed into wind-axis components
        Vector2 toolToPoint = closestPoint - toolPos;
        float alongTool = Vector2.Dot(toolToPoint, toolDir);

        // reject if closest point is behind the nozzle or beyond wind range
        if (alongTool < 0f || alongTool * alongTool > alongRadiusSq)
            return false;

        // forwardNormSq = squared normalised forward distance — 0 at nozzle, 1 at max range
        forwardNormSq = alongTool * alongTool / alongRadiusSq;

        // lateral component: remove the forward projection, what remains is perpendicular to the wind axis
        perp = toolToPoint - toolDir * alongTool;
        // lateralNormSq = squared normalised lateral distance — 0 on wind axis, 1 at cone edge
        lateralNormSq = perp.sqrMagnitude / perpRadiusSq;
        // their sum is the ellipse metric; ≤ 1 means inside the cone
        return forwardNormSq + lateralNormSq <= 1f;
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
            // wind cone: matches the normSum ellipse used in IsToolInRadius
            DrawEllipseGizmo(pos, hairDryer.transform.right, windRange, windWidth);
        }
    }
    void DrawEllipseGizmo(Vector2 center, Vector2 forward, float a, float b, int segments = 32)
    {
        Vector2 right = new(-forward.y, forward.x);
        // draw forward half-ellipse: t from -π/2 to π/2 (cos >= 0 = forward side)
        Vector3 start = center + right * -b;
        Vector3 prev  = start;
        for (int i = 1; i <= segments; i++)
        {
            float t    = Mathf.Lerp(-Mathf.PI * 0.5f, Mathf.PI * 0.5f, (float)i / segments);
            Vector3 next = (Vector3)(center + forward * (Mathf.Cos(t) * a) + right * (Mathf.Sin(t) * b));
            Gizmos.DrawLine(prev, next);
            prev = next;
        }
        // close with diameter line across the flat back
        Gizmos.DrawLine(prev, start);
    }
#endregion
}

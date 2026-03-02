using UnityEngine;

public class HairManager : MonoBehaviour
{
    [Header("Cards")]
    [SerializeField] int cardCount = 60;
    [SerializeField] float maxHeight = 1.6f;
    [SerializeField] float minHeight = 0.05f;
    [SerializeField] float cardWidth = 0.4f;
    [SerializeField] Sprite hairSprite;

    [Header("Head Arc")]
    [SerializeField] Vector2 headCenter = new Vector2(0f, 1.2f);
    [SerializeField] float arcRadius = 0.5f;
    [SerializeField] float arcMinDeg = -85f;
    [SerializeField] float arcMaxDeg = 85f;

    [Header("Wind")]
    [SerializeField] float windFreq = 2.5f;
    [SerializeField] float windStrength = 0.3f;
    [SerializeField] float windRadius = 1.5f;
    [SerializeField] float windSmooth = 5f;
    [SerializeField] float windRotFactor = 8f;

    Transform[] _cards;
    Vector3[] _restPos;
    Quaternion[] _baseRot;
    float[] _phaseOffset;
    float[] _amplitude;
    float[] _heightFraction;

    void Awake()
    {
        SpawnCards();
    }

    void Update()
    {
        UpdateCards();
    }

    // =========================================================
    // SPAWN
    // =========================================================

    void SpawnCards()
    {
        _cards = new Transform[cardCount];
        _restPos = new Vector3[cardCount];
        _baseRot = new Quaternion[cardCount];
        _phaseOffset = new float[cardCount];
        _amplitude = new float[cardCount];
        _heightFraction = new float[cardCount];

        for (int i = 0; i < cardCount; i++)
        {
            float t = i / (float)(cardCount - 1);
            float angle = Mathf.Lerp(arcMinDeg, arcMaxDeg, t);
            angle += Random.Range(-2f, 2f);

            float rad = angle * Mathf.Deg2Rad;

            Vector3 localPos = new Vector3(
                headCenter.x + Mathf.Sin(rad) * arcRadius,
                headCenter.y + Mathf.Cos(rad) * arcRadius,
                0f
            );

            GameObject go = new GameObject("Card_" + i);
            go.transform.SetParent(transform);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.identity;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = hairSprite;
            sr.sortingLayerName = "Hair";
            sr.sortingOrder = i;

            // pivot must be Bottom Center in sprite import
            go.transform.localScale = new Vector3(
                cardWidth,
                maxHeight,
                1f
            );

            _cards[i] = go.transform;
            _restPos[i] = localPos;
            _baseRot[i] = go.transform.localRotation;
            _phaseOffset[i] = Random.Range(0f, Mathf.PI * 2f);
            _amplitude[i] = 0f;
            _heightFraction[i] = Random.Range(0.85f, 1f);
        }
    }

    // =========================================================
    // UPDATE
    // =========================================================

    void UpdateCards()
    {
        for (int i = 0; i < cardCount; i++)
        {
            // Wind oscillation
            float x =
                Mathf.Sin(Time.time * windFreq + _phaseOffset[i])
                * _amplitude[i];

            Vector3 pos = _restPos[i];
            pos.x += x;

            _cards[i].localPosition = pos;

            _cards[i].localRotation =
                _baseRot[i] *
                Quaternion.Euler(0f, 0f, x * windRotFactor);

            // Apply height scaling (cut/grow)
            Vector3 scale = _cards[i].localScale;
            scale.y = maxHeight * _heightFraction[i];
            _cards[i].localScale = scale;

            // natural decay if no wind applied
            _amplitude[i] = Mathf.Lerp(
                _amplitude[i],
                0f,
                windSmooth * Time.deltaTime
            );
        }
    }

    // =========================================================
    // WIND
    // =========================================================

    public void ApplyWind(Vector2 dryerPos, float strength, float radius)
    {
        for (int i = 0; i < cardCount; i++)
        {
            float dist = Vector2.Distance(
                dryerPos,
                transform.TransformPoint(_restPos[i])
            );

            float target = 0f;

            if (dist < radius)
            {
                float falloff = 1f - (dist / radius);
                target = strength * falloff;
            }

            _amplitude[i] = Mathf.Lerp(
                _amplitude[i],
                target,
                windSmooth * Time.deltaTime
            );
        }
    }

    // =========================================================
    // CUT
    // =========================================================

    public void CutHair(Vector2 toolPos, float cutRadius)
    {
        for (int i = 0; i < cardCount; i++)
        {
            Vector2 worldPos =
                transform.TransformPoint(_restPos[i]);

            if (Vector2.Distance(toolPos, worldPos) > cutRadius)
                continue;

            float relativeY =
                (toolPos.y - worldPos.y) / maxHeight;

            float newFraction =
                Mathf.Clamp(relativeY, minHeight / maxHeight, 1f);

            if (newFraction < _heightFraction[i])
                _heightFraction[i] = newFraction;
        }
    }

    // =========================================================
    // GROW
    // =========================================================

    public void GrowHair(Vector2 toolPos, float growRadius, float growRate)
    {
        for (int i = 0; i < cardCount; i++)
        {
            Vector2 worldPos =
                transform.TransformPoint(_restPos[i]);

            float dist = Vector2.Distance(toolPos, worldPos);
            if (dist > growRadius) continue;

            float falloff = 1f - (dist / growRadius);

            _heightFraction[i] +=
                (growRate / maxHeight) *
                falloff *
                Time.deltaTime;

            _heightFraction[i] =
                Mathf.Clamp(_heightFraction[i], 0f, 1f);
        }
    }
}
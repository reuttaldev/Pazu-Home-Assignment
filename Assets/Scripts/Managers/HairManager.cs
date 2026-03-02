using UnityEngine;

public class HairManager : MonoBehaviour
{
    int cardCount = 60;
    float maxHeight = 1.7f;
    float minHeight = 0.05f;   // minimum after cut — never zero

    [Header("Head Arc")]
    [SerializeField] Vector2 headCenter = new Vector2(0f, 1.2f);
    [SerializeField] float arcRadius = 0.5f;
    [SerializeField] float arcMinDeg = -85f;
    [SerializeField] float arcMaxDeg = 85f;

    [Header("Wind")]
    [SerializeField] float windFreq = 2.5f;         // oscillation speed (radians/s)
    [SerializeField] float windSmooth = 5f;          // lerp speed for amplitude ramp-up and decay
    [SerializeField] float windRotFactor = 8f;       // degrees of card tilt per world unit of X offset

    [Header("References")]
    [SerializeField] GameObject[] cardPrefabs;

    Transform[] _cards;
    Animator[] _animators;
    Vector3[] _restPos;       // fixed world-space root, set at spawn
    Quaternion[] _baseRot;    // fixed outward-facing rotation, set at spawn
    float[] _phaseOffset;     // random per-card phase so cards don't oscillate in sync
    float[] _amplitude;       // current signed wind strength (negative = bends left)
    float[] _cardHeight;      // current height as fraction of maxHeight (0..1)

    void Awake()
    {
        _cards = new Transform[cardCount];
        _animators = new Animator[cardCount];
        _restPos = new Vector3[cardCount];
        _baseRot = new Quaternion[cardCount];
        _phaseOffset = new float[cardCount];
        _amplitude = new float[cardCount];
        _cardHeight = new float[cardCount];

        SpawnCards();
    }

    // Instantiates all cards along a semicircular arc at the top of the head
    void SpawnCards()
    {
        for (int i = 0; i < cardCount; i++)
        {
            float t = (float)i / (cardCount - 1); // normalized position in the arc
            float angle = Mathf.Lerp(arcMinDeg, arcMaxDeg, t); // find the angle that is t precent between the min and max degrees of possible hair placement
            angle += Random.Range(-3f, 3f);  // small jitter breaks visual symmetry

            float rad = angle * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Sin(rad), Mathf.Cos(rad));
            Vector2 pos = headCenter + dir * arcRadius;

            GameObject card = Instantiate(cardPrefabs[Random.Range(0, cardPrefabs.Length)], transform);
            card.name = "Card_" + i;


            card.transform.position = new Vector3(pos.x, pos.y, 0f);
            card.transform.rotation = Quaternion.Euler(0f, 0f, -angle);  // point outward from head
            
            // Taller cards spread slightly wider — mimics how long hair fans out
            float scaleVar = Random.Range(0.95f, 1.05f);
            float heightFrac = Random.Range(0.85f, 1.0f);
            float xScale = maxHeight * heightFrac * scaleVar;
            card.transform.localScale = new Vector3(xScale, 1f, 1f); 

            _cards[i] = card.transform;
            _animators[i] = card.GetComponent<Animator>();
            _restPos[i] = card.transform.position;
            _baseRot[i] = card.transform.rotation;
            _phaseOffset[i] = Random.Range(0f, 2f * Mathf.PI);
            _cardHeight[i] = heightFrac;
        }
    }

    void Update()
    {
        // Decay all amplitudes toward 0 every frame — cards settle when dryer is released
        for (int i = 0; i < cardCount; i++)
            _amplitude[i] = Mathf.Lerp(_amplitude[i], 0f, windSmooth * Time.deltaTime);

        UpdateCardTransforms();
    }

    // Applies sine-wave wind offset and tilt to each card based on its current amplitude
    void UpdateCardTransforms()
    {
        for (int i = 0; i < cardCount; i++)
        {
            float x = Mathf.Sin(Time.time * windFreq + _phaseOffset[i]) * _amplitude[i];
            float rot = x * windRotFactor;

            _cards[i].position = _restPos[i] + new Vector3(x, 0f, 0f);
            _cards[i].localEulerAngles = new Vector3(0f, 0f, _baseRot[i].eulerAngles.z + rot);

            _animators[i]?.SetBool("blow", Mathf.Abs(_amplitude[i]) > 0.01f);
        }
    }

    // Called by HairDryer each frame while dragging. Amplitude sign determines bend direction:
    // cards to the right of the dryer bend right, cards to the left bend left.
    public void ApplyWind(Vector2 dryerPos, float strength, float radius)
    {
        for (int i = 0; i < cardCount; i++)
        {
            float dist = Vector2.Distance(dryerPos, _restPos[i]);
            if (dist > radius) continue;

            float dx = _restPos[i].x - dryerPos.x;
            float side = Mathf.Abs(dx) > 0.01f ? Mathf.Sign(dx) : 1f;  // guard: Sign(0) = 0 causes flicker
            float falloff = 1f - (dist / radius);
            float targetAmp = strength * falloff * side;

            _amplitude[i] = Mathf.Lerp(_amplitude[i], targetAmp, windSmooth * Time.deltaTime);
        }
    }

    // Called by Scissors on a cooldown. Reduces card Y scale so the tip sits at toolPos.y.
    public void CutHair(Vector2 toolPos, float cutRadius)
    {
        for (int i = 0; i < cardCount; i++)
        {
            if (Mathf.Abs(toolPos.x - _restPos[i].x) > cutRadius + _cards[i].localScale.x * 0.5f)
                continue;

            // How much of the card remains — clamped to avoid negative scale if scissors go below root
            float raw = (toolPos.y - _restPos[i].y) / maxHeight;
            float newFraction = Mathf.Clamp(raw, minHeight / maxHeight, 1f);

            if (newFraction < _cardHeight[i])  // scissors can only cut, never grow
            {
                _cardHeight[i] = newFraction;
                Vector3 s = _cards[i].localScale;
                _cards[i].localScale = new Vector3(s.x, maxHeight * _cardHeight[i], 1f);
            }
        }
    }

    // Called by HairExtension each frame while dragging. Growth falls off with distance.
    public void GrowHair(Vector2 toolPos, float growRadius, float growRate)
    {
        for (int i = 0; i < cardCount; i++)
        {
            float dist = Vector2.Distance(toolPos, _restPos[i]);
            if (dist > growRadius) continue;

            float falloff = 1f - (dist / growRadius);
            _cardHeight[i] = Mathf.Min(_cardHeight[i] + (growRate / maxHeight) * falloff * Time.deltaTime, 1f);
            Vector3 s = _cards[i].localScale;
            _cards[i].localScale = new Vector3(s.x, maxHeight * _cardHeight[i], 1f);
        }
    }
}

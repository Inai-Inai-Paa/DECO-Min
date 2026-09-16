using System.Collections;
using TMPro;
using UnityEngine;

public class UITextCharacterAnimation : MonoBehaviour
{
    [SerializeField] private float _duration = 0.3f;
    [SerializeField] private float _characterDelay = 0.05f;
    [SerializeField] private float _scale = 1.3f;
    [SerializeField] private bool _loop = false;
    [SerializeField] private float _loopInterval = 0.2f;

    [SerializeField]
    private AnimationCurve _curve = new AnimationCurve(
        new Keyframe(0.0f, 0.0f),
        new Keyframe(0.5f, 1.0f),
        new Keyframe(1.0f, 0.0f)
    );

    private TMP_Text _text;
    private Coroutine _animationCoroutine;

    private void Awake()
    {
        _text = GetComponent<TMP_Text>();
    }

    public void Play()
    {
        Play(0);
    }

    public void Play(int startCharacterIndex)
    {
        if (_text == null)
        {
            return;
        }

        if (_animationCoroutine != null)
        {
            StopCoroutine(_animationCoroutine);
            _animationCoroutine = null;
        }

        _text.ForceMeshUpdate();

        _animationCoroutine = StartCoroutine(
            PlayWaveAnimation(startCharacterIndex)
        );
    }

    public void Stop()
    {
        if (_animationCoroutine != null)
        {
            StopCoroutine(_animationCoroutine);
            _animationCoroutine = null;
        }

        if (_text != null)
        {
            _text.ForceMeshUpdate();
        }
    }

    private IEnumerator PlayWaveAnimation(int startCharacterIndex)
    {
        _text.ForceMeshUpdate();

        TMP_TextInfo textInfo = _text.textInfo;
        TMP_MeshInfo[] originalMeshInfo =
            textInfo.CopyMeshInfoVertexData();

        do
        {
            RestoreMesh(
                textInfo,
                originalMeshInfo
            );

            float time = 0.0f;

            int animationCharacterCount =
                textInfo.characterCount - startCharacterIndex;

            float totalDuration =
                _duration + _characterDelay * Mathf.Max(
                    animationCharacterCount - 1,
                    0
                );

            while (time < totalDuration)
            {
                time += Time.deltaTime;

                for (int i = startCharacterIndex; i < textInfo.characterCount; i++)
                {
                    TMP_CharacterInfo characterInfo =
                        textInfo.characterInfo[i];

                    if (!characterInfo.isVisible)
                    {
                        continue;
                    }

                    float characterStartTime =
                        (i - startCharacterIndex) * _characterDelay;

                    float characterTime =
                        time - characterStartTime;

                    float rate = Mathf.Clamp01(
                        characterTime / _duration
                    );

                    float curveValue = 0.0f;

                    if (characterTime >= 0.0f &&
                        characterTime <= _duration)
                    {
                        curveValue = _curve.Evaluate(rate);
                    }

                    ApplyCharacterScale(
                        i,
                        curveValue,
                        textInfo,
                        originalMeshInfo
                    );
                }

                ApplyMesh(textInfo);

                yield return null;
            }

            RestoreMesh(
                textInfo,
                originalMeshInfo
            );

            if (_loop && _loopInterval > 0.0f)
            {
                yield return new WaitForSeconds(
                    _loopInterval
                );
            }
        }
        while (_loop);

        RestoreMesh(
            textInfo,
            originalMeshInfo
        );

        _animationCoroutine = null;
    }

    private void ApplyCharacterScale(
        int characterIndex,
        float curveValue,
        TMP_TextInfo textInfo,
        TMP_MeshInfo[] originalMeshInfo
    )
    {
        TMP_CharacterInfo characterInfo =
            textInfo.characterInfo[characterIndex];

        int materialIndex =
            characterInfo.materialReferenceIndex;

        int vertexIndex =
            characterInfo.vertexIndex;

        Vector3[] originalVertices =
            originalMeshInfo[materialIndex].vertices;

        Vector3[] vertices =
            textInfo.meshInfo[materialIndex].vertices;

        Vector3 center = (
            originalVertices[vertexIndex] +
            originalVertices[vertexIndex + 2]
        ) * 0.5f;

        float currentScale = Mathf.LerpUnclamped(
            1.0f,
            _scale,
            curveValue
        );

        for (int i = 0; i < 4; i++)
        {
            Vector3 offset =
                originalVertices[vertexIndex + i] - center;

            vertices[vertexIndex + i] =
                center + offset * currentScale;
        }
    }

    private void ApplyMesh(TMP_TextInfo textInfo)
    {
        for (int i = 0; i < textInfo.meshInfo.Length; i++)
        {
            TMP_MeshInfo meshInfo =
                textInfo.meshInfo[i];

            meshInfo.mesh.vertices =
                meshInfo.vertices;

            _text.UpdateGeometry(
                meshInfo.mesh,
                i
            );
        }
    }

    private void RestoreMesh(
        TMP_TextInfo textInfo,
        TMP_MeshInfo[] originalMeshInfo
    )
    {
        for (int i = 0; i < textInfo.meshInfo.Length; i++)
        {
            Vector3[] sourceVertices =
                originalMeshInfo[i].vertices;

            Vector3[] targetVertices =
                textInfo.meshInfo[i].vertices;

            if (sourceVertices == null ||
                targetVertices == null)
            {
                continue;
            }

            if (sourceVertices.Length != targetVertices.Length)
            {
                continue;
            }

            System.Array.Copy(
                sourceVertices,
                targetVertices,
                sourceVertices.Length
            );

            textInfo.meshInfo[i].mesh.vertices =
                targetVertices;

            _text.UpdateGeometry(
                textInfo.meshInfo[i].mesh,
                i
            );
        }
    }
}
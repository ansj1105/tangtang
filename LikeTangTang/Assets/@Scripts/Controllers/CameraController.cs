using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class CameraController : MonoBehaviour
{
    const float DefaultCameraSize = 24f;
    const float LateGameScreenScale = 0.9f;
    const float LateGameZoomDuration = 18f;


    public GameObject Target;
    public float Height { get; set; } = 0;
    public float Width { get; set; } = 0;

    float tickvalue = 5f;
    float adjust = 0.5f;
    bool isShake = false;
    Vector3 camPos;
    Camera targetCamera;
    Tween zoomTween;
    bool didLateGameZoomOut;

    private void Start()
    {
        targetCamera = GetComponent<Camera>();
        if (targetCamera == null)
            targetCamera = Camera.main;

        SetCameraSize();
        gameObject.GetOrAddComponent<MobileGameplayFrameRenderer>();
    }
    private void LateUpdate()
    {
        if (Target != null && Manager.GameM.CurrentMap != null && !isShake)
            LimitCameraArea();
    }

    void SetCameraSize()
    {
        SetCameraSize(DefaultCameraSize);
    }

    void SetCameraSize(float size)
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        if (targetCamera == null)
            return;

        targetCamera.orthographicSize = size;
        RefreshCameraBounds();
    }

    void RefreshCameraBounds()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        if (targetCamera == null)
            return;

        Height = targetCamera.orthographicSize;
        Width = Height * Screen.width / Screen.height;
    }

    public void ZoomOutForLateGame()
    {
        if (didLateGameZoomOut)
            return;

        if (targetCamera == null)
            targetCamera = Camera.main;

        if (targetCamera == null)
            return;

        didLateGameZoomOut = true;
        float lateGameCameraSize = targetCamera.orthographicSize / LateGameScreenScale;
        SmoothZoomTo(lateGameCameraSize, LateGameZoomDuration);
    }

    public void SmoothZoomTo(float targetSize, float duration)
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        if (targetCamera == null)
            return;

        zoomTween?.Kill();
        zoomTween = DOTween.To(
            () => targetCamera.orthographicSize,
            SetCameraSize,
            targetSize,
            duration)
            .SetEase(Ease.InOutSine);
    }

    void LimitCameraArea()
    {
        RefreshCameraBounds();
        transform.position = new Vector3(Target.transform.position.x, Target.transform.position.y, -10f);
        float limitX = Manager.GameM.CurrentMap.MapSize.x * 0.5f - Width;
        float clampX = Mathf.Clamp(transform.position.x, -limitX, limitX);

        float limitY = Manager.GameM.CurrentMap.MapSize.y * 0.5f - Height;
        float clampY = Mathf.Clamp(transform.position.y, -limitY, limitY);

        transform.position = new Vector3(clampX, clampY, -10f);
    }


    public void Shake()
    {
        if (!isShake)
            StartCoroutine(CoShake(0.25f));
    }

    IEnumerator CoShake(float _duration)
    {
        float halfDuration = _duration / 2;
        float elapsed = 0f;
        float tick = Random.Range(-10f, 10f);

        isShake = true;
        while(elapsed < _duration)
        {
            if (Manager.UiM.GetPopupCount() > 0) break;

            elapsed += Time.deltaTime / halfDuration;

            tick += Time.deltaTime * tickvalue;
            transform.position += new Vector3(
                Mathf.PerlinNoise(tick, 0) - 0.5f,
                Mathf.PerlinNoise(0, tick) - 0.5f,
                0f) * adjust * Mathf.PingPong(elapsed, halfDuration);

            yield return null;
        }

        isShake = false;
    }
}

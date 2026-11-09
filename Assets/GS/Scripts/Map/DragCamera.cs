using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DragCamera : MonoBehaviour
{
    [Header("拖拽设置")]
    public float dragSpeed = 1f;           // 拖拽速度
    public bool enableDrag = true;          // 是否启用拖拽

    [Header("边界限制")]
    public bool enableBounds = true;        // 是否启用边界限制
    public Vector2 minBounds = new Vector2(-100f, -100f);  // 最小边界 (X, Y)
    public Vector2 maxBounds = new Vector2(100f, 100f);    // 最大边界 (X, Y)

    [Header("缩放设置")]
    public bool enableZoom = true;          // 是否启用缩放
    public float zoomSpeed = 2f;            // 缩放速度
    public float minOrthographicSize = 5f;  // 最小正交大小（最远）
    public float maxOrthographicSize = 30f; // 最大正交大小（最近）

    private Vector3 dragOrigin;
    private bool isDragging = false;
    private Camera mainCamera;

    void Start()
    {
        mainCamera = GetComponent<Camera>();
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        // 如果不是正交相机，设置为正交
        if (!mainCamera.orthographic)
        {
            mainCamera.orthographic = true;
        }
    }

    void Update()
    {
        HandleDrag();
        HandleZoom();
    }

    void HandleDrag()
    {
        if (!enableDrag) return;

        // 鼠标左键按下
        if (Input.GetMouseButtonDown(0))
        {
            dragOrigin = GetMouseWorldPosition();
            isDragging = true;
        }

        // 鼠标左键抬起
        if (Input.GetMouseButtonUp(0))
        {
            isDragging = false;
        }

        // 拖拽中
        if (isDragging && Input.GetMouseButton(0))
        {
            Vector3 currentPos = GetMouseWorldPosition();
            Vector3 delta = dragOrigin - currentPos;

            transform.position += delta;

            // 应用边界限制
            if (enableBounds)
            {
                ClampCameraPosition();
            }
        }
    }

    void HandleZoom()
    {
        if (!enableZoom) return;

        // 鼠标滚轮缩放
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0)
        {
            float newSize = mainCamera.orthographicSize - scroll * zoomSpeed;
            mainCamera.orthographicSize = Mathf.Clamp(newSize, minOrthographicSize, maxOrthographicSize);
        }
    }

    Vector3 GetMouseWorldPosition()
    {
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = Mathf.Abs(transform.position.z - mainCamera.transform.position.z);
        return mainCamera.ScreenToWorldPoint(mousePos);
    }

    void ClampCameraPosition()
    {
        Vector3 pos = transform.position;

        // 获取相机可视范围的一半（基于正交大小和屏幕宽高比）
        float halfHeight = mainCamera.orthographicSize;
        float halfWidth = halfHeight * mainCamera.aspect;

        // 限制 X
        pos.x = Mathf.Clamp(pos.x, minBounds.x + halfWidth, maxBounds.x - halfWidth);
        // 限制 Y
        pos.y = Mathf.Clamp(pos.y, minBounds.y + halfHeight, maxBounds.y - halfHeight);

        transform.position = pos;
    }

    // 手动设置相机位置
    public void SetCameraPosition(Vector2 position)
    {
        transform.position = new Vector3(position.x, position.y, transform.position.z);
        if (enableBounds)
        {
            ClampCameraPosition();
        }
    }

    // 重置相机位置到中心
    public void ResetCameraPosition()
    {
        SetCameraPosition(Vector2.zero);
        mainCamera.orthographicSize = (minOrthographicSize + maxOrthographicSize) / 2;
    }

    // 在 Scene 视图中显示边界
    void OnDrawGizmosSelected()
    {
        if (!enableBounds) return;

        Gizmos.color = Color.green;
        Vector3 center = new Vector3((minBounds.x + maxBounds.x) / 2, (minBounds.y + maxBounds.y) / 2, 0);
        Vector3 size = new Vector3(maxBounds.x - minBounds.x, maxBounds.y - minBounds.y, 0);
        Gizmos.DrawWireCube(center, size);
    }
}
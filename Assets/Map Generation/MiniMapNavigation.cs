using UnityEngine;
using UnityEngine.EventSystems;

// Click or drag on the minimap to move the camera there (#18).
// Inverts MiniMap's mapping: icon anchor = (local position + boundary/2) / boundary, in MapsManager local space (x, z).
// Put this on a raycast-target Graphic that covers the map (the map panel); `miniMap` is the MiniMap whose rect defines the area.
public class MiniMapNavigation : MonoBehaviour, IPointerDownHandler, IDragHandler {

    public MiniMap miniMap;

    public void OnPointerDown(PointerEventData eventData) {
        PanTo(eventData);
    }

    public void OnDrag(PointerEventData eventData) {
        PanTo(eventData);
    }

    private void PanTo(PointerEventData eventData) {
        RectTransform area = (RectTransform)miniMap.transform;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(area, eventData.position, eventData.pressEventCamera, out Vector2 local)) {
            return;
        }

        Rect rect = area.rect;
        float percentX = Mathf.Clamp01((local.x - rect.xMin) / rect.width);
        float percentY = Mathf.Clamp01((local.y - rect.yMin) / rect.height);

        MapsManager mapsManager = Script.Get<MapsManager>();
        Rect bounds = mapsManager.mapsBoundaries;
        Vector3 localPoint = new Vector3(percentX * bounds.width - bounds.width / 2f, 0, percentY * bounds.height - bounds.height / 2f);
        Vector3 world = mapsManager.transform.TransformPoint(localPoint);

        Script.Get<PlayerBehaviour>().PanCameraToPosition(world, null, false);
    }
}

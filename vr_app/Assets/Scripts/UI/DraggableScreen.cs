using UnityEngine;
using UnityEngine.EventSystems;

namespace OmniBot.VR.UI
{
    /// <summary>
    /// Allows a world-space UI Canvas to be dragged around freely in 3D space
    /// using either the VR pointer or hand pinch.
    /// </summary>
    public class DraggableScreen : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private Transform _targetTransform;
        private Vector3 _offset;
        private Camera _mainCamera;
        private float _zDistance;

        private void Awake()
        {
            // We drag the root Canvas transform
            _targetTransform = transform;
            _mainCamera = Camera.main;
        }

        private System.Collections.IEnumerator Start()
        {
            if (_mainCamera == null) _mainCamera = Camera.main;
            
            // Wait until the VR camera is actually positioned above the floor
            while (_mainCamera != null && _mainCamera.transform.position.y < 0.5f)
            {
                yield return null;
            }

            if (_mainCamera != null)
            {
                // Snap to head height, preserving the distance in X and Z
                Vector3 newPos = _targetTransform.position;
                newPos.y = _mainCamera.transform.position.y;
                _targetTransform.position = newPos;
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (_mainCamera == null) _mainCamera = Camera.main;

            // Get the world position of the raycast hit
            if (eventData.pointerCurrentRaycast.isValid)
            {
                Vector3 hitPoint = eventData.pointerCurrentRaycast.worldPosition;
                _zDistance = Vector3.Distance(_mainCamera.transform.position, hitPoint);
                _offset = _targetTransform.position - hitPoint;
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_mainCamera == null) return;

            // Project the pointer's ray out to the same distance
            Ray pointerRay;
            // OVRInputModule usually provides world-space rays in eventData if pointerDrag is set
            if (eventData.enterEventCamera != null)
            {
                pointerRay = eventData.enterEventCamera.ScreenPointToRay(eventData.position);
            }
            else
            {
                // Fallback: estimate from camera
                pointerRay = _mainCamera.ScreenPointToRay(eventData.position);
            }

            Vector3 newHitPoint = pointerRay.origin + pointerRay.direction * _zDistance;
            
            // Move the screen
            _targetTransform.position = newHitPoint + _offset;

            // Keep the screen facing the user
            FaceUser();
        }

        public void OnEndDrag(PointerEventData eventData)
        {
        }

        private void FaceUser()
        {
            Vector3 toCam = _targetTransform.position - _mainCamera.transform.position;
            toCam.y = 0f;
            if (toCam.sqrMagnitude > 1e-4f)
            {
                _targetTransform.rotation = Quaternion.LookRotation(toCam.normalized, Vector3.up);
            }
        }
    }
}

﻿using System;
using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Gerard.CherrypickGames
{
    public class SpawnerController : MonoBehaviour
    {
        [SerializeField] private Item itemPrefab;

        private GridManager _gridManager;
        private bool _isDragging;
        private Camera _mainCamera;
        private Collider2D _collider2D;
        private Vector3 _originalPositionBeforeDrag;

        private Action _onAllItemsSpawned;
        private Action<Vector2Int> _onSpawnerMoved;

        private void Awake()
        {
            _collider2D = GetComponent<Collider2D>();
        }

        public void Initialize(GridManager gridManager, Camera mainCamera, Action onAllItemsSpawned,
            Action<Vector2Int> onSpawnerMoved)
        {
            _gridManager = gridManager;
            _mainCamera = mainCamera;
            _onAllItemsSpawned = onAllItemsSpawned;
            _onSpawnerMoved = onSpawnerMoved;
        }

        public void Spawn()
        {
            if (_gridManager.OrderedStack == null) return;

            var possibleColors = _gridManager.PossibleColors;
            while (_gridManager.OrderedStack.Count > 0)
            {
                var targetGridPosition = _gridManager.OrderedStack.Pop();
                if (_gridManager.IsValidPosition(targetGridPosition))
                {
                    var targetPosition = _gridManager.GetCell(targetGridPosition).transform.position;
                    var item = Instantiate(itemPrefab, transform.position, Quaternion.identity, _gridManager.transform);
                    var color = possibleColors[Random.Range(0, possibleColors.Count)];
                    _gridManager.GetCell(targetGridPosition).AddItem(item, color);

                    // Start animation towards target cell
                    StartCoroutine(MoveItemToTargetPosition(item.transform, targetPosition, .1f));
                    return;
                }
            }

            _onAllItemsSpawned.Invoke();
        }

        private IEnumerator MoveItemToTargetPosition(Transform itemTransform, Vector3 targetPosition, float duration)
        {
            var startPosition = itemTransform.position;
            float elapsed = 0;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var normalizedTime = elapsed / duration; // value between 0 and 1
                itemTransform.position = Vector3.Lerp(startPosition, targetPosition, normalizedTime);
                yield return null;
            }

            itemTransform.position = targetPosition; // ensure the item reaches the exact target position
        }

        public bool IsMouseOverOrDragging()
        {
            var mouseWorldPosition = _mainCamera.ScreenToWorldPoint(Input.mousePosition);
            return _collider2D.OverlapPoint(mouseWorldPosition) || _isDragging;
        }

        public void HandleDrag()
        {
            var mouseWorldPosition = _mainCamera.ScreenToWorldPoint(Input.mousePosition);
            if (Input.GetMouseButtonDown(0))
            {
                _originalPositionBeforeDrag = transform.position;
                _isDragging = true;
            }

            if (Input.GetMouseButtonUp(0) && _isDragging)
            {
                if (SnapToCell(mouseWorldPosition, out var newGridPosition))
                {
                    _onSpawnerMoved?.Invoke(newGridPosition);
                }

                _isDragging = false;
            }

            if (_isDragging)
            {
                mouseWorldPosition.z = 0; // Ensure the z-position remains consistent
                transform.position = mouseWorldPosition;
            }
        }

        private bool SnapToCell(Vector3 mousePosition, out Vector2Int newGridPosition)
        {
            var closestX = Mathf.RoundToInt(mousePosition.x + _gridManager.XOffset);
            var closestY =
                Mathf.RoundToInt(_gridManager.Height - 1 - mousePosition.y -
                                 _gridManager.YOffset); // Adjust Y calculation

            // Ensure we are within grid boundaries
            closestX = Mathf.Clamp(closestX, 0, _gridManager.Width - 1);
            closestY = Mathf.Clamp(closestY, 0, _gridManager.Height - 1);

            newGridPosition = new Vector2Int(closestX, closestY);
            var targetCell = _gridManager.GetCell(newGridPosition);

            if (targetCell.IsBlocked || !targetCell.IsEmpty)
            {
                transform.position = _originalPositionBeforeDrag;
            }
            else
            {
                transform.position = targetCell.transform.position;
            }

            // If current position is different than original means the snapping was successful
            return transform.position != _originalPositionBeforeDrag;
        }
    }
}
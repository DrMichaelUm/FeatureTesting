using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitController : MonoBehaviour
{
    public bool IsKing => this.name.Contains("King");
    private float speed = 5f;

    private Vector3 m_targetDirection = Vector3.zero;
    
    public Vector3 CorrectionDirection { get; set; } = Vector3.zero;
    public float PotentialValue { get; set; } = 0f;
    public float MaxPotentialValue { get; set; } = 1f;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    Vector3 inputDirection = Vector3.zero;
    void Update()
    {
        inputDirection = GetInputDirection();

        if (Math.Abs(inputDirection.magnitude) < 0.01f)
            Stop();

        //m_targetDirection += inputDirection;
        //AddToTargetDirection(CorrectionDirection);
        LerpToTargetDirection(CorrectionDirection, PotentialValue, MaxPotentialValue);
        if (m_targetDirection.magnitude > 1)
            m_targetDirection.Normalize();
        
        SimpleMovement(m_targetDirection);
        
        //m_targetDirection = Vector3.zero;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(this.transform.position + Vector3.up * 2f, m_targetDirection);
        Gizmos.color = Color.white;
        
        Gizmos.color = Color.green;
        Gizmos.DrawRay(this.transform.position + Vector3.up * 2f, inputDirection);
        Gizmos.color = Color.white;

    }

    public void AddToTargetDirection(Vector3 correctionDirection)
    {
        Vector3 inputDirection = GetInputDirection();

        m_targetDirection = inputDirection + correctionDirection;
        
        if (m_targetDirection.magnitude > 1)
            m_targetDirection.Normalize();
    }

    public void LerpToTargetDirection (Vector3 correctionDirection, float potentialValue, float maxPotentialValue)
    {
        Vector3 inputDirection = GetInputDirection();
        m_targetDirection = Vector3.Lerp(inputDirection, correctionDirection, potentialValue/maxPotentialValue);
        if (inputDirection.magnitude != 0 && Math.Abs(m_targetDirection.magnitude) < 0.01f)
            Debug.Log(potentialValue);

        // if (m_targetDirection.magnitude > 1)
        //     m_targetDirection.Normalize();
    }

    private void Stop()
    {
        m_targetDirection = Vector3.zero;
    }
    private void SimpleMovement(Vector3 targetDirection)
    {
        targetDirection = targetDirection * (speed * Time.deltaTime);
        transform.Translate(targetDirection);
    }

    private Vector3 GetInputDirection() => new Vector3(Input.GetAxis("Horizontal") , 0, Input.GetAxis("Vertical"));
}

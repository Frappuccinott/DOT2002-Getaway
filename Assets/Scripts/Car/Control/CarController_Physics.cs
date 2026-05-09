using UnityEngine;

public partial class CarController
{
    private void FixedUpdate()
    {
        ApplyPhysics();
    }

    private void ApplyPhysics()
    {
        float speedKMH = rb.linearVelocity.magnitude * 3.6f;
        float forwardDot = Vector3.Dot(transform.forward, rb.linearVelocity);

        if (currentShiftTimer > 0) currentShiftTimer -= Time.fixedDeltaTime;

        float targetSteerAngle = moveInput.x * maxSteeringAngle;
        
        if (Mathf.Abs(moveInput.x) < 0.1f && speedKMH > 1f)
            currentSteerAngle = Mathf.MoveTowards(currentSteerAngle, 0f, Time.fixedDeltaTime * autoCenterSpeed);
        else
            currentSteerAngle = Mathf.Lerp(currentSteerAngle, targetSteerAngle, Time.fixedDeltaTime * steeringSmoothness);
        
        if (frontLeftWC.gameObject.activeInHierarchy && frontLeftWC.enabled) frontLeftWC.steerAngle = currentSteerAngle;
        if (frontRightWC.gameObject.activeInHierarchy && frontRightWC.enabled) frontRightWC.steerAngle = currentSteerAngle;

        float acceleration = moveInput.y;
        bool isMovingForward = forwardDot > 0.1f;
        
        if (isHandbrakeEngaged)
        {
            SetMotorTorque(rearLeftWC, 0f);
            SetMotorTorque(rearRightWC, 0f);
            SetBrakeTorque(rearLeftWC, brakeTorque);
            SetBrakeTorque(rearRightWC, brakeTorque);
        }
        else
        {
            SetBrakeTorque(rearLeftWC, 0f);
            SetBrakeTorque(rearRightWC, 0f);

            if (isMovingForward && acceleration > 0 && speedKMH >= maxSpeedForward)
            {
                SetMotorTorque(rearLeftWC, 0f);
                SetMotorTorque(rearRightWC, 0f);
            }
            else if (!isMovingForward && acceleration < 0 && speedKMH >= maxSpeedReverse)
            {
                SetMotorTorque(rearLeftWC, 0f);
                SetMotorTorque(rearRightWC, 0f);
            }
            else if (currentShiftTimer > 0)
            {
                SetMotorTorque(rearLeftWC, 0f);
                SetMotorTorque(rearRightWC, 0f);
            }
            else if (currentFuelLiters <= 0f || currentBatteryPercent <= 0f || (carStartSystem != null && !carStartSystem.IsRunning))
            {
                SetMotorTorque(rearLeftWC, 0f);
                SetMotorTorque(rearRightWC, 0f);
                float appliedBrake = (moveInput.y < 0) ? brakeTorque : (brakeTorque * 0.5f);
                SetBrakeTorque(rearLeftWC, appliedBrake);
                SetBrakeTorque(rearRightWC, appliedBrake);
                displayRPM = Mathf.Lerp(displayRPM, 0f, Time.fixedDeltaTime * 2f);
            }
            else
            {
                SetMotorTorque(rearLeftWC, acceleration * motorTorque);
                SetMotorTorque(rearRightWC, acceleration * motorTorque);

                bool isBraking = false;
                if (speedKMH > 1f)
                {
                    if (isMovingForward && acceleration < 0) isBraking = true;
                    if (!isMovingForward && acceleration > 0) isBraking = true;
                }

                if (isBraking)
                {
                    SetBrakeTorque(rearLeftWC, brakeTorque);
                    SetBrakeTorque(rearRightWC, brakeTorque);
                }
                else
                {
                    SetBrakeTorque(rearLeftWC, 0f);
                    SetBrakeTorque(rearRightWC, 0f);
                }
            }
        }

        displaySpeed = speedKMH;
        CalculateHUDData(acceleration, isMovingForward, speedKMH);
        UpdateAnalogDials();
        ConsumeResources(speedKMH);
        ApplyWeightTransfer(acceleration, targetSteerAngle);
    }

    private void ApplyWeightTransfer(float acceleration, float steerAngle)
    {
        if (carBody == null) return;

        float speedKMH = rb.linearVelocity.magnitude * 3.6f;

        if (currentShiftTimer > 0)
        {
            targetBodyPitch = gearShiftJoltForce;
        }
        else
        {
            if (acceleration != 0)
            {
                targetBodyPitch = Mathf.Clamp(-acceleration * bodyPitchMultiplier, -bodyPitchMultiplier, bodyPitchMultiplier);
            }
            else 
            {
                if (speedKMH > 5f && moveInput.y < 0) 
                    targetBodyPitch = Mathf.Clamp(brakeTorque * 0.001f * bodyPitchMultiplier, -bodyPitchMultiplier, bodyPitchMultiplier);
                else 
                    targetBodyPitch = 0f;
            }
        }

        targetBodyRoll = Mathf.Clamp(-currentSteerAngle / maxSteeringAngle * bodyRollMultiplier * (speedKMH / 50f), -bodyRollMultiplier, bodyRollMultiplier);

        Quaternion targetRotation = Quaternion.Euler(targetBodyPitch, 0f, targetBodyRoll);
        float smoothness = (currentShiftTimer > 0) ? bodySmoothness * 3f : bodySmoothness;
        carBody.localRotation = Quaternion.Slerp(carBody.localRotation, targetRotation, Time.fixedDeltaTime * smoothness);
    }

    private void UpdateWheelVisuals()
    {
        UpdateSingleWheel(frontLeftWC, frontLeftMesh);
        UpdateSingleWheel(frontRightWC, frontRightMesh);
        UpdateSingleWheel(rearLeftWC, rearLeftMesh);
        UpdateSingleWheel(rearRightWC, rearRightMesh);
    }

    private void UpdateSingleWheel(WheelCollider wc, Transform mesh)
    {
        if (!mesh) return;
        if (wc != null && wc.gameObject.activeInHierarchy && wc.enabled)
        {
            wc.GetWorldPose(out Vector3 position, out Quaternion rotation);
            mesh.SetPositionAndRotation(position, rotation);
        }
    }

    private void SetMotorTorque(WheelCollider wc, float torque)
    {
        if (wc != null && wc.gameObject.activeInHierarchy && wc.enabled)
            wc.motorTorque = torque;
    }

    private void SetBrakeTorque(WheelCollider wc, float torque)
    {
        if (wc != null && wc.gameObject.activeInHierarchy && wc.enabled)
            wc.brakeTorque = torque;
    }
}

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
        
        frontLeftWC.steerAngle = currentSteerAngle;
        frontRightWC.steerAngle = currentSteerAngle;

        float acceleration = moveInput.y;
        bool isMovingForward = forwardDot > -0.5f;
        
        if (isHandbrakeEngaged)
        {
            rearLeftWC.motorTorque = 0f;
            rearRightWC.motorTorque = 0f;
            rearLeftWC.brakeTorque = brakeTorque;
            rearRightWC.brakeTorque = brakeTorque;
        }
        else
        {
            if (isMovingForward && acceleration > 0 && speedKMH >= maxSpeedForward)
            {
                rearLeftWC.motorTorque = 0f;
                rearRightWC.motorTorque = 0f;
            }
            else if (!isMovingForward && acceleration < 0 && speedKMH >= maxSpeedReverse)
            {
                rearLeftWC.motorTorque = 0f;
                rearRightWC.motorTorque = 0f;
            }
            else if (currentShiftTimer > 0)
            {
                rearLeftWC.motorTorque = 0f;
                rearRightWC.motorTorque = 0f;
            }
            else if (currentFuelLiters <= 0f || currentBatteryPercent <= 0f || (carStartSystem != null && !carStartSystem.IsRunning))
            {
                rearLeftWC.motorTorque = 0f;
                rearRightWC.motorTorque = 0f;
                rearLeftWC.brakeTorque = brakeTorque * 0.5f;
                rearRightWC.brakeTorque = brakeTorque * 0.5f;
                displayRPM = Mathf.Lerp(displayRPM, 0f, Time.fixedDeltaTime * 2f);
            }
            else
            {
                rearLeftWC.motorTorque = acceleration * motorTorque;
                rearRightWC.motorTorque = acceleration * motorTorque;
            }

            if (currentFuelLiters > 0f && currentBatteryPercent > 0f)
            {
                rearLeftWC.brakeTorque = 0f;
                rearRightWC.brakeTorque = 0f;
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
        wc.GetWorldPose(out Vector3 position, out Quaternion rotation);
        mesh.SetPositionAndRotation(position, rotation);
    }
}

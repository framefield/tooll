// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Collections.Generic;
using SharpDX;

namespace Framefield.Core.OperatorPartTraits
{

    public interface ICameraProvider
    {
        Vector3 GetLastPosition();
        void SetPosition(double time, Vector3 pos);

        Vector3 GetLastTarget();
        void SetTarget(double time, Vector3 target);

        double GetLastRoll();
        void SetRoll(double time, double roll);

        double CalculateFOV(OperatorPartContext context);
        double GetLastFOV();

        Matrix CalculateWorldToCamera(OperatorPartContext context);
        Matrix GetLastWorldToCamera();

        Matrix CalculateCameraToView(OperatorPartContext context);
        Matrix GetLastCameraToView();

        Vector2 GetLastNearFarClip();
    }

}

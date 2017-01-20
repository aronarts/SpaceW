﻿#region License
// Procedural planet generator.
// 
// Copyright (C) 2015-2017 Denis Ovchinnikov [zameran] 
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions
// are met:
// 1. Redistributions of source code must retain the above copyright
//    notice, this list of conditions and the following disclaimer.
// 2. Redistributions in binary form must reproduce the above copyright
//    notice, this list of conditions and the following disclaimer in the
//    documentation and/or other materials provided with the distribution.
// 3. Neither the name of the copyright holders nor the names of its
//    contributors may be used to endorse or promote products derived from
//    this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
// AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
// IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
// ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE
// LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
// CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
// SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
// INTERRUPTION)HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
// CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
// ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF
// THE POSSIBILITY OF SUCH DAMAGE.
// 
// Creation Date: Undefined
// Creation Time: Undefined
// Creator: zameran
#endregion

using System;

using UnityEngine;

public static class CameraHelper
{
    public static Camera Main()
    {
        return Camera.main;
    }

    public static Camera DepthCamera()
    {
        if (Camera.main.gameObject.transform.FindChild("CustomDepthCamera") != null)
            if (Camera.main.gameObject.transform.FindChild("CustomDepthCamera").GetComponent<Camera>() != null)
                return Camera.main.gameObject.transform.FindChild("CustomDepthCamera").GetComponent<Camera>();

        return null;
    }

    public static Matrix4x4 GetWorldToCamera(this Camera camera)
    {
        return camera.worldToCameraMatrix;
    }

    public static Matrix4x4 GetCameraToWorld(this Camera camera)
    {
        return camera.cameraToWorldMatrix;
    }

    public static Matrix4x4 GetCameraToScreen(this Camera camera, bool useFix = true)
    {
        var projectionMatrix = camera.projectionMatrix;

        if (!useFix) return projectionMatrix;

        if (SystemInfo.graphicsDeviceVersion.IndexOf("Direct3D") > -1)
        {
            // NOTE : Default unity antialiasing breaks matrices?
            if (camera.actualRenderingPath == RenderingPath.DeferredLighting || camera.actualRenderingPath == RenderingPath.DeferredShading || QualitySettings.antiAliasing == 0)
            {
                // Invert Y for rendering to a render texture
                for (byte i = 0; i < 4; i++)
                {
                    projectionMatrix[1, i] = -projectionMatrix[1, i];
                }
            }

            // Scale and bias depth range
            for (byte i = 0; i < 4; i++)
            {
                projectionMatrix[2, i] = projectionMatrix[2, i] * 0.5f + projectionMatrix[3, i] * 0.5f;
            }
        }

        return projectionMatrix;
    }

    public static Matrix4x4 GetScreenToCamera(this Camera camera)
    {
        return camera.GetCameraToScreen().inverse;
    }

    public static Matrix4x4 GetScreenToCamera(this Camera camera, bool useFix)
    {
        return camera.GetCameraToScreen(useFix).inverse;
    }

    public static Vector3 GetProjectedDirection(this Vector3 v)
    {
        var cameraToWorld = Main().GetCameraToWorld();
        var screenToCamera = Main().GetScreenToCamera();

        return cameraToWorld.MultiplyPoint(screenToCamera.MultiplyPoint(v));
    }

    public static Vector3 GetRelativeProjectedDirection(this Vector3 v, Matrix4x4 worldToLocal)
    {
        return worldToLocal.MultiplyPoint(v.GetProjectedDirection());
    }

    public static void WithReplacedProjection(Action ToDo)
    {
        var camera = Main();
        var projectionMatrix = camera.projectionMatrix;

        camera.projectionMatrix = camera.GetCameraToScreen();
        if (ToDo != null) ToDo();
        camera.projectionMatrix = projectionMatrix;
    }
}
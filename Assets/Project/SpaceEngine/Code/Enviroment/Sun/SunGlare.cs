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


using UnityEngine;

namespace SpaceEngine.AtmosphericScattering.Sun
{
    public sealed class SunGlare : Node<SunGlare>
    {
        private CachedComponent<AtmosphereSun> SunCachedComponent = new CachedComponent<AtmosphereSun>();

        public AtmosphereSun SunComponent { get { return SunCachedComponent.Component; } }

        public Atmosphere Atmosphere;

        public Shader SunGlareShader;
        private Material SunGlareMaterial;

        public SunGlareSettings Settings;

        public EngineRenderQueue RenderQueue = EngineRenderQueue.Transparent;
        public int RenderQueueOffset = 1000000; //NOTE : Render over all.

        public float Magnitude = 1;

        public bool InitUniformsInUpdate = true;

        private bool Eclipse = false;

        private Vector3 ViewPortPosition = Vector3.zero;

        private float Scale = 1;
        private float Fade = 1;

        public AnimationCurve FadeCurve = new AnimationCurve(new Keyframe[] { new Keyframe(0.0f, 0.0f),
                                                                              new Keyframe(1.0f, 1.0f),
                                                                              new Keyframe(10.0f, 0.0f) });

        private Mesh SunGlareMesh;

        private Matrix4x4 Ghost1Settings = Matrix4x4.zero;
        private Matrix4x4 Ghost2Settings = Matrix4x4.zero;
        private Matrix4x4 Ghost3Settings = Matrix4x4.zero;

        #region Node

        protected override void InitNode()
        {
            if (Settings == null) return;

            SunGlareMaterial = MaterialHelper.CreateTemp(SunGlareShader, "Sunglare", (int)RenderQueue);

            SunGlareMesh = MeshFactory.MakePlane(8, 8, MeshFactory.PLANE.XY, false, false, false);
            SunGlareMesh.bounds = new Bounds(Vector4.zero, new Vector3(9e37f, 9e37f, 9e37f));

            for (int i = 0; i < Settings.Ghost1SettingsList.Count; i++)
                Ghost1Settings.SetRow(i, Settings.Ghost1SettingsList[i]);

            for (int i = 0; i < Settings.Ghost2SettingsList.Count; i++)
                Ghost2Settings.SetRow(i, Settings.Ghost2SettingsList[i]);

            for (int i = 0; i < Settings.Ghost3SettingsList.Count; i++)
                Ghost3Settings.SetRow(i, Settings.Ghost3SettingsList[i]);

            InitUniforms(SunGlareMaterial);
        }

        protected override void UpdateNode()
        {
            if (Settings == null) return;

            SunGlareMaterial.renderQueue = (int)RenderQueue + RenderQueueOffset;

            var distance = (CameraHelper.Main().transform.position.normalized - SunComponent.transform.position.normalized).magnitude;

            ViewPortPosition = CameraHelper.Main().WorldToViewportPoint(SunComponent.transform.position);

            // NOTE : So, camera's projection matrix replacement is bad idea in fact of strange clip planes behaviour.
            // Instead i will invert the y component of resulting vector of WorldToViewportPoint.
            // Looks like better idea...
            if (CameraHelper.Main().IsDeferred())
            {
                ViewPortPosition.y = 1.0f - ViewPortPosition.y;
            }

            Scale = distance / Magnitude;
            Fade = FadeCurve.Evaluate(Mathf.Clamp(Scale, 0.0f, 100.0f));
            //Fade = FadeCurve.Evaluate(Mathf.Clamp01(VectorHelper.AngularRadius(SunComponent.transform.position, CameraHelper.Main().transform.position, 250000.0f)));

            //RaycastHit hit;

            Eclipse = false;

            //Eclipse = Physics.Raycast(CameraHelper.Main().transform.position, (SunComponent.transform.position - CameraHelper.Main().transform.position).normalized, out hit, Mathf.Infinity);
            //if (!Eclipse)
            //    Eclipse = Physics.Raycast(CameraHelper.Main().transform.position, (SunComponent.transform.position - CameraHelper.Main().transform.position).normalized, out hit, Mathf.Infinity);

            if (InitUniformsInUpdate) InitUniforms(SunGlareMaterial);

            SetUniforms(SunGlareMaterial);
        }

        protected override void Start()
        {
            SunCachedComponent.TryInit(this);

            base.Start();
        }

        protected override void Update()
        {
            if (ViewPortPosition.z > 0)
            {
                if (Atmosphere == null) return;

                Graphics.DrawMesh(SunGlareMesh, Vector3.zero, Quaternion.identity, SunGlareMaterial, 10, CameraHelper.Main(), 0, Atmosphere.planetoid.QuadMPB, false, false);
            }

            base.Update();
        }

        #endregion

        public void InitSetAtmosphereUniforms()
        {
            InitUniforms(SunGlareMaterial);
            SetUniforms(SunGlareMaterial);
        }

        public void InitUniforms(Material mat)
        {
            if (mat == null) return;

            SunGlareMaterial.SetTexture("sunSpikes", Settings.SunSpikes);
            SunGlareMaterial.SetTexture("sunFlare", Settings.SunFlare);
            SunGlareMaterial.SetTexture("sunGhost1", Settings.SunGhost1);
            SunGlareMaterial.SetTexture("sunGhost2", Settings.SunGhost2);
            SunGlareMaterial.SetTexture("sunGhost3", Settings.SunGhost3);

            SunGlareMaterial.SetVector("flareSettings", Settings.FlareSettings);
            SunGlareMaterial.SetVector("spikesSettings", Settings.SpikesSettings);
            SunGlareMaterial.SetMatrix("ghost1Settings", Ghost1Settings);
            SunGlareMaterial.SetMatrix("ghost2Settings", Ghost2Settings);
            SunGlareMaterial.SetMatrix("ghost3Settings", Ghost2Settings);

            if (Atmosphere != null) Atmosphere.InitUniforms(null, SunGlareMaterial, false);
        }

        public void SetUniforms(Material mat)
        {
            if (mat == null) return;

            SunGlareMaterial.SetVector("sunViewPortPos", ViewPortPosition);

            SunGlareMaterial.SetFloat("AspectRatio", CameraHelper.Main().aspect);
            SunGlareMaterial.SetFloat("Scale", Scale);
            SunGlareMaterial.SetFloat("Fade", Fade);
            SunGlareMaterial.SetFloat("UseAtmosphereColors", 1.0f);
            SunGlareMaterial.SetFloat("UseRadiance", 0.0f);
            SunGlareMaterial.SetFloat("Eclipse", Eclipse ? 0.0f : 1.0f);

            SunGlareMaterial.renderQueue = (int)RenderQueue + RenderQueueOffset;

            if (Atmosphere != null) { Atmosphere.SetUniforms(null, SunGlareMaterial, false, false); }
        }
    }
}
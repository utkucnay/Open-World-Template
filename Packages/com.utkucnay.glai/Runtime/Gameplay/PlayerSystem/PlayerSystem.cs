using Glai.ECS;
using Unity.Mathematics;
using UnityEngine;

namespace Glai.Gameplay
{
    public class PlayerSystem : System
    {
        Entity cameraEntity;
        PlayerSystemConfig config;
        float yaw;
        float pitch;

        public PlayerSystem(Entity playerEntity) : this(playerEntity, PlayerSystemConfig.Default)
        {
        }

        public PlayerSystem(Entity playerEntity, PlayerSystemConfig config)
        {
            this.cameraEntity = playerEntity;
            this.config = config;
        }

        public override void Start()
        {
            if (Camera.main == null)
            {
                Debug.LogError("Main Camera not found. Please ensure there is a camera in the scene with the tag 'MainCamera'.");
                return;
            }

            var cameraTransform = Camera.main.transform;
            cameraTransform.SetPositionAndRotation(config.DefaultCameraPosition, Quaternion.LookRotation(config.LookTarget - config.DefaultCameraPosition, Vector3.up));
            var rotation = cameraTransform.rotation;
            var eulerAngles = rotation.eulerAngles;

            yaw = eulerAngles.y;
            pitch = NormalizePitch(eulerAngles.x);

            ECSAPI.GetComponentRef<TransformComponent>(cameraEntity).transform = Matrix4x4.TRS(
                cameraTransform.position,
                quaternion.Euler(math.radians(new float3(pitch, yaw, 0f))),
                Vector3.one);
        }
        
        public override void Tick(float deltaTime)
        {
            ref var transform = ref ECSAPI.GetComponentRef<TransformComponent>(cameraEntity);
            bool isLooking = Input.GetMouseButton(1);

            Cursor.lockState = isLooking ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !isLooking;

            if (isLooking)
            {
                var mouseDelta = new float2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
                yaw += mouseDelta.x * config.LookSensitivity * deltaTime;
                pitch = math.clamp(pitch - mouseDelta.y * config.LookSensitivity * deltaTime, config.MinPitch, config.MaxPitch);
            }

            quaternion rotation = quaternion.Euler(math.radians(new float3(pitch, yaw, 0f)));
            float3 forward = math.mul(rotation, new float3(0f, 0f, 1f));
            float3 right = math.mul(rotation, new float3(1f, 0f, 0f));
            float3 moveDirection = float3.zero;

            if (isLooking && Input.GetKey(KeyCode.W))
            {
                moveDirection += forward;
            }

            if (isLooking && Input.GetKey(KeyCode.S))
            {
                moveDirection -= forward;
            }

            if (isLooking && Input.GetKey(KeyCode.A))
            {
                moveDirection -= right;
            }

            if (isLooking && Input.GetKey(KeyCode.D))
            {
                moveDirection += right;
            }

            if (isLooking && Input.GetKey(KeyCode.E))
            {
                moveDirection += new float3(0f, 1f, 0f);
            }

            if (isLooking && Input.GetKey(KeyCode.Q))
            {
                moveDirection -= new float3(0f, 1f, 0f);
            }

            if (!moveDirection.Equals(float3.zero))
            {
                float speed = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)
                    ? config.MoveSpeed * config.FastMoveMultiplier
                    : config.MoveSpeed;

                transform.position += math.normalizesafe(moveDirection) * speed * deltaTime;
            }

            transform.transform = Matrix4x4.TRS(transform.position, rotation, Vector3.one);
        }

        public override void LateTick(float deltaTime)
        {
            ref var transform = ref ECSAPI.GetComponentRef<TransformComponent>(cameraEntity);
            Camera.main.transform.SetPositionAndRotation(transform.position, math.quaternion(transform.transform));
        }

        static float NormalizePitch(float pitch)
        {
            return pitch > 180f ? pitch - 360f : pitch;
        }
    }
}

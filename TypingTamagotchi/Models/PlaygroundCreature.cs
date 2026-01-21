using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace TypingTamagotchi.Models;

public partial class PlaygroundCreature : ObservableObject
{
    public Creature Creature { get; set; } = null!;

    // 위치 (픽셀)
    [ObservableProperty]
    private double _x;

    [ObservableProperty]
    private double _y;

    // 속도
    public double VelocityX { get; set; }
    public double VelocityY { get; set; }

    // 점프 관련
    public bool IsOnGround { get; set; } = true;
    public double GroundY { get; set; }

    // 방향 (1: 오른쪽, -1: 왼쪽)
    [ObservableProperty]
    private double _direction = 1;

    // 충돌 상태
    [ObservableProperty]
    private double _rotation;

    [ObservableProperty]
    private double _scaleY = 1.0;

    // 그림자 크기 (점프 높이에 따라)
    [ObservableProperty]
    private double _shadowScale = 1.0;

    // 충돌 복구 타이머
    public double RecoveryTimer { get; set; }
    public bool IsKnockedOver { get; set; }
    public bool IsSquashed { get; set; }

    // 물리 상수
    public const double Gravity = 800; // 중력 가속도
    public const double JumpVelocity = -350; // 점프 초기 속도
    public const double MoveSpeed = 80; // 좌우 이동 속도
    public const double BounceFactor = 0.6; // 바닥 튕김 계수

    // 크리처 크기 (충돌 검출용)
    public const double Width = 48;
    public const double Height = 48;

    public void Update(double deltaTime, double minX, double maxX, double groundY)
    {
        // 복구 타이머 처리
        if (RecoveryTimer > 0)
        {
            RecoveryTimer -= deltaTime;
            if (RecoveryTimer <= 0)
            {
                // 복구
                if (IsKnockedOver)
                {
                    IsKnockedOver = false;
                    Rotation = 0;
                }
                if (IsSquashed)
                {
                    IsSquashed = false;
                    ScaleY = 1.0;
                }
            }
        }

        // 넘어진 상태면 움직임 제한
        if (IsKnockedOver || IsSquashed)
        {
            // Y 물리만 적용 (중력)
            VelocityY += Gravity * deltaTime;
            Y += VelocityY * deltaTime;

            if (Y >= groundY)
            {
                Y = groundY;
                VelocityY = 0;
                IsOnGround = true;
            }
            return;
        }

        // 좌우 이동
        X += VelocityX * deltaTime;

        // 벽에 부딪히면 방향 전환
        if (X <= minX)
        {
            X = minX;
            VelocityX = Math.Abs(VelocityX);
            Direction = 1;
        }
        else if (X >= maxX - Width)
        {
            X = maxX - Width;
            VelocityX = -Math.Abs(VelocityX);
            Direction = -1;
        }

        // 중력 적용
        VelocityY += Gravity * deltaTime;
        Y += VelocityY * deltaTime;

        // 바닥 충돌
        if (Y >= groundY)
        {
            Y = groundY;
            IsOnGround = true;

            // 튕기기 (일정 속도 이상이면)
            if (VelocityY > 100)
            {
                VelocityY = -VelocityY * BounceFactor;
                IsOnGround = false;
            }
            else
            {
                VelocityY = 0;

                // 바닥에 있을 때 랜덤 점프
                if (Random.Shared.NextDouble() < 0.02) // 2% 확률로 점프
                {
                    Jump();
                }
            }
        }
        else
        {
            IsOnGround = false;
        }

        // 그림자 크기 업데이트 (점프 높이에 따라)
        double jumpHeight = groundY - Y;
        ShadowScale = Math.Max(0.3, 1.0 - jumpHeight / 100.0);
    }

    public void Jump()
    {
        if (IsOnGround && !IsKnockedOver && !IsSquashed)
        {
            VelocityY = JumpVelocity;
            IsOnGround = false;
        }
    }

    public void KnockOver()
    {
        IsKnockedOver = true;
        Rotation = Direction > 0 ? 90 : -90;
        RecoveryTimer = 1.5; // 1.5초 후 복구
    }

    public void Squash()
    {
        IsSquashed = true;
        ScaleY = 0.8;
        RecoveryTimer = 0.4; // 0.4초 후 복구
    }

    public void BounceUp()
    {
        VelocityY = JumpVelocity * 1.2; // 밟고 뛰면 더 높이
        IsOnGround = false;
    }
}

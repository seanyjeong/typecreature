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

    [ObservableProperty]
    private double _groundY;

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

    // 그림자 X 위치 (크리처 중앙 아래)
    public double ShadowX => X + 8; // 그림자 중앙 맞추기 (96px 크리처, 80px 그림자)

    // 그림자 Y 위치 (바닥 - 그림자 높이의 절반)
    public double ShadowY => GroundY - 10;

    // 시각적 Y 위치 (스프라이트 상단 기준)
    public double VisualY => Y - Height;

    // 시각적 X 위치 (Direction에 따른 보정)
    // ScaleX=-1로 뒤집으면 이미지가 왼쪽으로 48px 이동하므로 보정
    public double VisualX => Direction < 0 ? X + Width : X;

    partial void OnXChanged(double value)
    {
        OnPropertyChanged(nameof(ShadowX));
        OnPropertyChanged(nameof(VisualX));
    }

    partial void OnYChanged(double value)
    {
        OnPropertyChanged(nameof(VisualY));
    }

    partial void OnGroundYChanged(double value)
    {
        OnPropertyChanged(nameof(ShadowY));
    }

    partial void OnDirectionChanged(double value)
    {
        OnPropertyChanged(nameof(VisualX));
    }

    // 충돌 복구 타이머
    public double RecoveryTimer { get; set; }
    public bool IsKnockedOver { get; set; }
    public bool IsSquashed { get; set; }

    // 이동 패턴
    public enum MovePattern { Walk, Float, Bounce, Idle }
    public MovePattern CurrentPattern { get; set; } = MovePattern.Walk;
    public double PatternTimer { get; set; }
    public double IdleJumpTimer { get; set; }

    // 물리 상수
    public const double Gravity = 800; // 중력 가속도
    public const double JumpVelocity = -350; // 점프 초기 속도
    public const double MoveSpeed = 80; // 좌우 이동 속도
    public const double BounceFactor = 0.6; // 바닥 튕김 계수
    public const double FloatSpeed = 40; // 둥둥 떠다니기 속도

    // 크리처 크기 (충돌 검출용) - 놀이터에서 2배 크기
    public const double Width = 96;
    public const double Height = 96;

    public void Update(double deltaTime, double minX, double maxX, double groundY)
    {
        // 바닥 Y 위치 업데이트 (그림자 바인딩용)
        GroundY = groundY;

        // 복구 타이머 처리
        if (RecoveryTimer > 0)
        {
            RecoveryTimer -= deltaTime;
            if (RecoveryTimer <= 0)
            {
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

        // 패턴 타이머 및 전환
        PatternTimer -= deltaTime;
        if (PatternTimer <= 0)
        {
            ChangePattern();
        }

        // 패턴별 행동
        switch (CurrentPattern)
        {
            case MovePattern.Walk:
                UpdateWalk(deltaTime, minX, maxX, groundY);
                break;
            case MovePattern.Float:
                UpdateFloat(deltaTime, minX, maxX, groundY);
                break;
            case MovePattern.Bounce:
                UpdateBounce(deltaTime, minX, maxX, groundY);
                break;
            case MovePattern.Idle:
                UpdateIdle(deltaTime, groundY);
                break;
        }

        // 벽 충돌 처리 (공통)
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

        // 그림자 크기 업데이트 (점프 높이에 따라)
        double jumpHeight = Math.Max(0, groundY - Y);
        ShadowScale = Math.Max(0.3, 1.0 - jumpHeight / 80.0);
    }

    private void ChangePattern()
    {
        var rand = Random.Shared.NextDouble();
        if (rand < 0.35)
            CurrentPattern = MovePattern.Walk;
        else if (rand < 0.55)
            CurrentPattern = MovePattern.Float;
        else if (rand < 0.80)
            CurrentPattern = MovePattern.Bounce;
        else
            CurrentPattern = MovePattern.Idle;

        PatternTimer = 2.0 + Random.Shared.NextDouble() * 4.0; // 2~6초

        // 패턴별 초기화
        if (CurrentPattern == MovePattern.Walk)
        {
            VelocityX = (Random.Shared.NextDouble() - 0.5) * MoveSpeed * 2;
            Direction = VelocityX > 0 ? 1 : -1;
        }
        else if (CurrentPattern == MovePattern.Float)
        {
            VelocityX = (Random.Shared.NextDouble() - 0.5) * FloatSpeed * 2;
            // VelocityY는 그대로 유지 (부드러운 전환)
            Direction = VelocityX > 0 ? 1 : -1;
        }
        else if (CurrentPattern == MovePattern.Bounce)
        {
            VelocityX = (Random.Shared.NextDouble() - 0.5) * MoveSpeed * 1.5;
            Direction = VelocityX > 0 ? 1 : -1;
        }
        else if (CurrentPattern == MovePattern.Idle)
        {
            VelocityX = 0;
            IdleJumpTimer = 0.5 + Random.Shared.NextDouble() * 1.0;
        }
    }

    private void UpdateWalk(double deltaTime, double minX, double maxX, double groundY)
    {
        // 랜덤 방향 전환 (3% 확률)
        if (Random.Shared.NextDouble() < 0.03 * deltaTime * 60)
        {
            VelocityX = -VelocityX;
            Direction = VelocityX > 0 ? 1 : -1;
        }

        X += VelocityX * deltaTime;
        VelocityY += Gravity * deltaTime;
        Y += VelocityY * deltaTime;

        if (Y >= groundY)
        {
            Y = groundY;
            VelocityY = 0;
            IsOnGround = true;

            // 가끔 점프
            if (Random.Shared.NextDouble() < 0.01)
                Jump();
        }
        else
        {
            IsOnGround = false;
        }
    }

    private void UpdateFloat(double deltaTime, double minX, double maxX, double groundY)
    {
        // 둥둥 떠다니기 (부드럽게)
        X += VelocityX * deltaTime;

        // 부드러운 위아래 움직임
        double targetY = groundY - 30; // 바닥에서 30px 위가 목표
        double diff = targetY - Y;

        // 목표 위치로 부드럽게 이동 (스프링 효과)
        VelocityY += diff * 3 * deltaTime; // 스프링 힘
        VelocityY *= 0.98; // 감쇠

        Y += VelocityY * deltaTime;

        // 범위 제한 (부드럽게)
        if (Y < groundY - 70)
        {
            Y = groundY - 70;
            VelocityY = Math.Max(0, VelocityY);
        }
        if (Y > groundY)
        {
            Y = groundY;
            VelocityY = Math.Min(0, VelocityY);
        }

        IsOnGround = false;
    }

    private void UpdateBounce(double deltaTime, double minX, double maxX, double groundY)
    {
        X += VelocityX * deltaTime;
        VelocityY += Gravity * deltaTime;
        Y += VelocityY * deltaTime;

        if (Y >= groundY)
        {
            Y = groundY;
            IsOnGround = true;
            // 계속 튀기
            VelocityY = JumpVelocity * (0.6 + Random.Shared.NextDouble() * 0.4);
            IsOnGround = false;
        }
        else
        {
            IsOnGround = false;
        }
    }

    private void UpdateIdle(double deltaTime, double groundY)
    {
        // 제자리에서 가끔 점프
        VelocityY += Gravity * deltaTime;
        Y += VelocityY * deltaTime;

        if (Y >= groundY)
        {
            Y = groundY;
            VelocityY = 0;
            IsOnGround = true;

            IdleJumpTimer -= deltaTime;
            if (IdleJumpTimer <= 0)
            {
                VelocityY = JumpVelocity * (0.5 + Random.Shared.NextDouble() * 0.3);
                IsOnGround = false;
                IdleJumpTimer = 0.8 + Random.Shared.NextDouble() * 1.5;
            }
        }
        else
        {
            IsOnGround = false;
        }
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

using System;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TypingTamagotchi.Models;
using TypingTamagotchi.Services;

namespace TypingTamagotchi.ViewModels;

public partial class DesktopPetViewModel : ViewModelBase
{
    private readonly DesktopPetService _petService;
    private readonly DesktopPet _pet;
    private readonly DispatcherTimer _animationTimer;

    public event Action? RemoveRequested;
    public event Action? DragStarted;
    public event Action? DragEnded;

    [ObservableProperty]
    private double _x;

    [ObservableProperty]
    private double _y;

    [ObservableProperty]
    private string _spritePath = "";

    [ObservableProperty]
    private string _creatureName = "";

    [ObservableProperty]
    private bool _isFacingRight = true;

    [ObservableProperty]
    private double _animationOffsetY;

    [ObservableProperty]
    private double _animationRotation;

    [ObservableProperty]
    private double _animationScale = 1.0;

    [ObservableProperty]
    private bool _isContextMenuOpen;

    public DesktopPet Pet => _pet;
    public int CreatureId => _pet.Creature.Id;

    public DesktopPetViewModel(DesktopPetService petService, DesktopPet pet)
    {
        _petService = petService;
        _pet = pet;

        X = pet.X;
        Y = pet.Y;
        SpritePath = pet.Creature.SpritePath;
        CreatureName = pet.Creature.Name;
        IsFacingRight = pet.FacingRight;

        _animationTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(16) // ~60fps
        };
        _animationTimer.Tick += OnAnimationTick;
        _animationTimer.Start();
    }

    private void OnAnimationTick(object? sender, EventArgs e)
    {
        // 상태에 따른 애니메이션 (회전 없음, 바운스만)
        switch (_pet.State)
        {
            case PetState.Idle:
                // 살짝 위아래로 떠다니는 효과
                AnimationOffsetY = Math.Sin(_pet.AnimationFrame) * 2;
                AnimationRotation = 0;
                AnimationScale = 1.0;
                break;

            case PetState.Walking:
                // 걷는 효과 - 바운스만
                AnimationOffsetY = Math.Abs(Math.Sin(_pet.AnimationFrame * 4)) * 5;
                AnimationRotation = 0;
                AnimationScale = 1.0;
                break;

            case PetState.Sitting:
                // 앉아있기
                AnimationOffsetY = 2;
                AnimationRotation = 0;
                AnimationScale = 0.95;
                break;

            case PetState.Greeting:
                // 인사 - 살짝 숙임
                AnimationOffsetY = Math.Sin(_pet.AnimationFrame * 6) * 3;
                AnimationRotation = 0;
                AnimationScale = 1.0;
                break;

            case PetState.Dragging:
                // 발버둥 - 바운스만
                AnimationOffsetY = Math.Sin(_pet.AnimationFrame * 10) * 4;
                AnimationRotation = 0;
                AnimationScale = 1.0;
                break;

            case PetState.Clicked:
                // 점프
                AnimationOffsetY = -8 * Math.Sin(_pet.StateTimer * Math.PI / 0.5);
                AnimationRotation = 0;
                AnimationScale = 1.05;
                break;
        }

        // 위치 동기화
        X = _pet.X;
        Y = _pet.Y + AnimationOffsetY;
        IsFacingRight = _pet.FacingRight;
    }

    public void OnClicked()
    {
        _petService.OnPetClicked(_pet);
    }

    public void OnDragStart()
    {
        _petService.StartDragging(_pet);
        DragStarted?.Invoke();
    }

    public void OnDrag(double deltaX, double deltaY, double screenWidth, double screenHeight)
    {
        _pet.X = Math.Clamp(_pet.X + deltaX, 0, screenWidth - DesktopPetService.PetSize);
        _pet.Y = Math.Clamp(_pet.Y + deltaY, 0, screenHeight - DesktopPetService.PetSize);
    }

    public void OnDragEnd(double screenHeight)
    {
        _petService.StopDragging(_pet, screenHeight);
        DragEnded?.Invoke();
    }

    [RelayCommand]
    private void RemoveFromDesktop()
    {
        _animationTimer.Stop();
        _petService.RemovePet(_pet);
        RemoveRequested?.Invoke();
    }

    public void Cleanup()
    {
        _animationTimer.Stop();
    }
}

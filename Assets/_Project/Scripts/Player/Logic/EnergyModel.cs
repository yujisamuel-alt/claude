using System;

namespace Enxada.Player
{
    /// <summary>Números da energia, em ScriptableObject no Unity e aqui como lógica pura.</summary>
    public sealed class EnergySettings
    {
        public int MaxEnergy { get; }

        /// <summary>Abaixo disto o jogador desmaia (padrão: -15).</summary>
        public int PassOutThreshold { get; }

        /// <summary>Multiplicador de velocidade com energia 0 ou menos.</summary>
        public float ExhaustedSpeedMultiplier { get; }

        /// <summary>Fração da energia máxima com que o jogador acorda depois de desmaiar.</summary>
        public float PassOutRecoveryFraction { get; }

        public EnergySettings(int maxEnergy = 270, int passOutThreshold = -15, float exhaustedSpeedMultiplier = 0.5f,
            float passOutRecoveryFraction = 0.5f)
        {
            if (maxEnergy <= 0)
                throw new ArgumentException("A energia máxima precisa ser positiva.");
            if (passOutThreshold > 0)
                throw new ArgumentException("O limite de desmaio não pode ser positivo.");
            if (exhaustedSpeedMultiplier <= 0f || exhaustedSpeedMultiplier > 1f)
                throw new ArgumentException("O multiplicador de velocidade vai de 0 (exclusivo) a 1.");
            if (passOutRecoveryFraction < 0f || passOutRecoveryFraction > 1f)
                throw new ArgumentException("A recuperação ao desmaiar é uma fração entre 0 e 1.");

            MaxEnergy = maxEnergy;
            PassOutThreshold = passOutThreshold;
            ExhaustedSpeedMultiplier = exhaustedSpeedMultiplier;
            PassOutRecoveryFraction = passOutRecoveryFraction;
        }
    }

    /// <summary>
    /// Energia do jogador. Com 0 ou menos ele fica lento (mas ainda pode trabalhar);
    /// abaixo do limite de desmaio, o evento Exhausted pede para o dia terminar.
    /// </summary>
    public sealed class EnergyModel
    {
        private bool _exhaustedRaised;

        public EnergyModel(EnergySettings settings)
        {
            Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            Current = settings.MaxEnergy;
        }

        public EnergySettings Settings { get; }
        public int Current { get; private set; }
        public int Max => Settings.MaxEnergy;

        /// <summary>Sem energia: lento, mas continua jogando.</summary>
        public bool IsDepleted => Current <= 0;

        /// <summary>Abaixo do limite: vai desmaiar.</summary>
        public bool IsPassedOut => Current < Settings.PassOutThreshold;

        /// <summary>0 a 1, para a barra (energia negativa mostra a barra vazia).</summary>
        public float Fraction => Math.Max(0, Current) / (float)Max;

        public float SpeedMultiplier => IsDepleted ? Settings.ExhaustedSpeedMultiplier : 1f;

        public event Action Changed;

        /// <summary>Disparado uma vez quando a energia passa do limite de desmaio.</summary>
        public event Action Exhausted;

        public void Spend(int amount)
        {
            if (amount <= 0)
                return;

            Current -= amount;
            Changed?.Invoke();
            RaiseExhaustedIfNeeded();
        }

        /// <summary>Recupera energia (comida, por exemplo), sem passar do máximo.</summary>
        public void Restore(int amount)
        {
            if (amount <= 0)
                return;

            SetCurrent(Math.Min(Max, Current + amount));
        }

        /// <summary>Começo de um novo dia: dormir recupera tudo; desmaiar, só uma fração.</summary>
        public void NewDay(bool passedOut)
        {
            SetCurrent(passedOut ? (int)Math.Floor(Max * (double)Settings.PassOutRecoveryFraction) : Max);
        }

        /// <summary>Define o valor direto (carregar save).</summary>
        public void SetCurrent(int value)
        {
            Current = Math.Min(Max, value);
            if (!IsPassedOut)
                _exhaustedRaised = false;

            Changed?.Invoke();
            RaiseExhaustedIfNeeded();
        }

        private void RaiseExhaustedIfNeeded()
        {
            if (!IsPassedOut || _exhaustedRaised)
                return;

            _exhaustedRaised = true;
            Exhausted?.Invoke();
        }
    }
}

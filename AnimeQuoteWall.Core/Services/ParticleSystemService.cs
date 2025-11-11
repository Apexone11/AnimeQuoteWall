using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace AnimeQuoteWall.Core.Services;

/// <summary>
/// Represents a single particle in the particle system.
/// </summary>
public class Particle
{
    public float X { get; set; }
    public float Y { get; set; }
    public float VelocityX { get; set; }
    public float VelocityY { get; set; }
    public Color Color { get; set; }
    public float Size { get; set; }
    public float Lifetime { get; set; }
    public float MaxLifetime { get; set; }
    public float Opacity { get; set; } = 1.0f;
}

/// <summary>
/// Particle emitter configuration.
/// </summary>
public class ParticleEmitter
{
    public float SpawnRate { get; set; } = 1.0f; // Particles per second
    public string ParticleType { get; set; } = "Snow";
    public RectangleF SpawnArea { get; set; }
    public Color ParticleColor { get; set; } = Color.White;
    public float MinSize { get; set; } = 2f;
    public float MaxSize { get; set; } = 8f;
    public float MinSpeed { get; set; } = 10f;
    public float MaxSpeed { get; set; } = 50f;
    public float MinLifetime { get; set; } = 5f;
    public float MaxLifetime { get; set; } = 15f;
}

/// <summary>
/// Service for managing particle systems (snow, stars, sparkles, etc.).
/// </summary>
public class ParticleSystemService
{
    private readonly List<Particle> _particles = new();
    private readonly Random _random = new();
    private float _spawnTimer = 0f;

    /// <summary>
    /// Updates all particles based on elapsed time.
    /// </summary>
    public void UpdateParticles(float deltaTime, int screenWidth, int screenHeight, ParticleEmitter emitter)
    {
        // Spawn new particles
        _spawnTimer += deltaTime;
        var spawnInterval = 1.0f / emitter.SpawnRate;
        
        while (_spawnTimer >= spawnInterval)
        {
            SpawnParticle(emitter, screenWidth, screenHeight);
            _spawnTimer -= spawnInterval;
        }

        // Update existing particles
        for (int i = _particles.Count - 1; i >= 0; i--)
        {
            var particle = _particles[i];
            
            // Update position
            particle.X += particle.VelocityX * deltaTime;
            particle.Y += particle.VelocityY * deltaTime;
            
            // Update lifetime
            particle.Lifetime -= deltaTime;
            particle.Opacity = Math.Max(0f, particle.Lifetime / particle.MaxLifetime);
            
            // Apply particle type specific behavior
            ApplyParticleBehavior(particle, emitter.ParticleType, screenWidth, screenHeight);
            
            // Remove dead particles
            if (particle.Lifetime <= 0 || particle.Y > screenHeight + 50 || particle.X < -50 || particle.X > screenWidth + 50)
            {
                _particles.RemoveAt(i);
            }
        }
    }

    /// <summary>
    /// Spawns a new particle based on emitter settings.
    /// </summary>
    private void SpawnParticle(ParticleEmitter emitter, int screenWidth, int screenHeight)
    {
        var particle = new Particle
        {
            X = emitter.SpawnArea.X + (float)(_random.NextDouble() * emitter.SpawnArea.Width),
            Y = emitter.SpawnArea.Y + (float)(_random.NextDouble() * emitter.SpawnArea.Height),
            Size = emitter.MinSize + (float)(_random.NextDouble() * (emitter.MaxSize - emitter.MinSize)),
            Color = emitter.ParticleColor,
            MaxLifetime = emitter.MinLifetime + (float)(_random.NextDouble() * (emitter.MaxLifetime - emitter.MinLifetime)),
            Lifetime = emitter.MinLifetime + (float)(_random.NextDouble() * (emitter.MaxLifetime - emitter.MinLifetime)),
            Opacity = 1.0f
        };

        // Set velocity based on particle type
        switch (emitter.ParticleType.ToLowerInvariant())
        {
            case "snow":
                particle.VelocityX = (float)(_random.NextDouble() - 0.5) * 20f;
                particle.VelocityY = emitter.MinSpeed + (float)(_random.NextDouble() * (emitter.MaxSpeed - emitter.MinSpeed));
                break;
            case "rain":
                particle.VelocityX = (float)(_random.NextDouble() - 0.5) * 10f;
                particle.VelocityY = emitter.MinSpeed + (float)(_random.NextDouble() * (emitter.MaxSpeed - emitter.MinSpeed)) * 2f;
                break;
            case "stars":
                particle.VelocityX = (float)(_random.NextDouble() - 0.5) * 5f;
                particle.VelocityY = (float)(_random.NextDouble() - 0.5) * 5f;
                break;
            case "sparkles":
                var angle = (float)(_random.NextDouble() * Math.PI * 2);
                var speed = emitter.MinSpeed + (float)(_random.NextDouble() * (emitter.MaxSpeed - emitter.MinSpeed));
                particle.VelocityX = (float)Math.Cos(angle) * speed;
                particle.VelocityY = (float)Math.Sin(angle) * speed;
                break;
            default:
                particle.VelocityX = (float)(_random.NextDouble() - 0.5) * emitter.MaxSpeed;
                particle.VelocityY = (float)(_random.NextDouble() - 0.5) * emitter.MaxSpeed;
                break;
        }

        _particles.Add(particle);
    }

    /// <summary>
    /// Applies particle type specific behavior.
    /// </summary>
    private void ApplyParticleBehavior(Particle particle, string particleType, int screenWidth, int screenHeight)
    {
        switch (particleType.ToLowerInvariant())
        {
            case "snow":
                // Snow falls down and drifts slightly
                if (particle.Y > screenHeight)
                {
                    particle.Y = -10;
                    particle.X = (float)(_random.NextDouble() * screenWidth);
                }
                break;
            case "rain":
                // Rain falls straight down
                if (particle.Y > screenHeight)
                {
                    particle.Y = -10;
                    particle.X = (float)(_random.NextDouble() * screenWidth);
                }
                break;
            case "stars":
                // Stars twinkle (vary opacity)
                particle.Opacity = 0.5f + 0.5f * (float)Math.Sin(particle.MaxLifetime - particle.Lifetime);
                break;
            case "sparkles":
                // Sparkles fade out
                break;
        }
    }

    /// <summary>
    /// Draws all particles on the graphics surface.
    /// </summary>
    public void DrawParticles(Graphics graphics)
    {
        foreach (var particle in _particles)
        {
            var color = Color.FromArgb((int)(particle.Opacity * 255), particle.Color);
            using var brush = new SolidBrush(color);
            graphics.FillEllipse(brush, particle.X - particle.Size / 2, particle.Y - particle.Size / 2, particle.Size, particle.Size);
        }
    }

    /// <summary>
    /// Clears all particles.
    /// </summary>
    public void ClearParticles()
    {
        _particles.Clear();
    }

    /// <summary>
    /// Gets the current particle count.
    /// </summary>
    public int ParticleCount => _particles.Count;
}


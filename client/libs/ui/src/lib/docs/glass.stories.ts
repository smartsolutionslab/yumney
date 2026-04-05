import { Meta, StoryObj } from '@storybook/angular';
import { Component } from '@angular/core';

@Component({
  selector: 'yn-glass-docs',
  standalone: true,
  template: `
    <div class="glass-docs">
      <section>
        <h2>Glass Surfaces — Elevation Levels</h2>
        <p class="subtitle">
          Each level increases background opacity, border visibility, and shadow depth.
        </p>
        <div class="ambient-bg">
          <div class="glass-grid">
            @for (level of levels; track level.name) {
              <div class="glass-demo" [class]="'glass-level-' + level.level">
                <h3>{{ level.name }}</h3>
                <p>Elevation {{ level.level }}</p>
                <code>&#64;include glass-surface({{ level.level }})</code>
              </div>
            }
          </div>
        </div>
      </section>

      <section>
        <h2>Semantic Glass Mixins</h2>
        <div class="ambient-bg">
          <div class="semantic-demos">
            <div class="glass-card-demo">
              <h3>glass-card</h3>
              <p>For content cards and sections</p>
            </div>
            <div class="glass-header-demo">
              <span>glass-header — sticky navigation bar</span>
            </div>
            <div class="glass-dialog-demo">
              <h3>glass-dialog</h3>
              <p>For modals and confirmation dialogs</p>
            </div>
          </div>
        </div>
      </section>

      <section>
        <h2>Ambient Background Orbs</h2>
        <div class="ambient-preview">
          <div class="orb orb-1"></div>
          <div class="orb orb-2"></div>
          <div class="orb orb-3"></div>
          <div class="glass-on-ambient">
            <p>Glass card on ambient background</p>
          </div>
        </div>
      </section>
    </div>
  `,
  styles: [
    `
      .glass-docs {
        max-width: 800px;
      }
      section {
        margin-bottom: 2.5rem;
      }
      h2 {
        font-size: 1.25rem;
        font-weight: 600;
        margin-bottom: 0.5rem;
        color: var(--yn-text);
      }
      .subtitle {
        font-size: 0.875rem;
        color: var(--yn-text-muted);
        margin-bottom: 1rem;
      }
      h3 {
        font-size: 1rem;
        font-weight: 600;
        margin-bottom: 0.25rem;
      }
      p {
        font-size: 0.875rem;
        color: var(--yn-text-secondary);
        margin-bottom: 0.5rem;
      }
      code {
        font-size: 0.75rem;
        font-family: var(--yn-font-mono);
        color: var(--yn-text-muted);
      }

      .ambient-bg {
        padding: 2rem;
        border-radius: 16px;
        position: relative;
        background:
          radial-gradient(ellipse 300px 300px at 20% 30%, var(--yn-ambient-orb-1), transparent),
          radial-gradient(ellipse 250px 250px at 75% 60%, var(--yn-ambient-orb-2), transparent),
          radial-gradient(ellipse 200px 200px at 50% 80%, var(--yn-ambient-orb-3), transparent),
          var(--yn-background);
      }

      .glass-grid {
        display: grid;
        grid-template-columns: repeat(2, 1fr);
        gap: 1rem;
      }

      .glass-demo {
        padding: 1.5rem;
        border-radius: 12px;
        transform: translateZ(0);
        backdrop-filter: blur(12px);
        -webkit-backdrop-filter: blur(12px);
      }
      .glass-level-0 {
        background: rgb(255 255 255 / 10%);
        border: 1px solid rgb(255 255 255 / 15%);
      }
      .glass-level-1 {
        background: rgb(255 255 255 / 10%);
        border: 1px solid rgb(255 255 255 / 25%);
        box-shadow:
          0 1px 3px rgb(0 0 0 / 6%),
          0 2px 8px rgb(0 0 0 / 4%);
      }
      .glass-level-2 {
        background: rgb(255 255 255 / 15%);
        border: 1px solid rgb(255 255 255 / 25%);
        box-shadow:
          0 2px 8px rgb(0 0 0 / 8%),
          0 8px 24px rgb(0 0 0 / 6%);
      }
      .glass-level-3 {
        background: rgb(255 255 255 / 20%);
        border: 1px solid rgb(255 255 255 / 25%);
        box-shadow:
          0 4px 16px rgb(0 0 0 / 12%),
          0 12px 32px rgb(0 0 0 / 8%);
        backdrop-filter: blur(20px);
        -webkit-backdrop-filter: blur(20px);
      }

      .semantic-demos {
        display: flex;
        flex-direction: column;
        gap: 1rem;
      }
      .glass-card-demo {
        padding: 1.5rem;
        border-radius: 12px;
        background: rgb(255 255 255 / 10%);
        border: 1px solid rgb(255 255 255 / 25%);
        box-shadow:
          0 1px 3px rgb(0 0 0 / 6%),
          0 2px 8px rgb(0 0 0 / 4%);
        backdrop-filter: blur(12px);
        -webkit-backdrop-filter: blur(12px);
      }
      .glass-header-demo {
        padding: 0.75rem 1rem;
        background: rgb(255 255 255 / 10%);
        border-bottom: 1px solid rgb(255 255 255 / 25%);
        box-shadow:
          0 1px 3px rgb(0 0 0 / 6%),
          0 2px 8px rgb(0 0 0 / 4%);
        backdrop-filter: blur(12px);
        -webkit-backdrop-filter: blur(12px);
        font-size: 0.875rem;
      }
      .glass-dialog-demo {
        padding: 2rem;
        border-radius: 12px;
        background: rgb(255 255 255 / 20%);
        border: 1px solid rgb(255 255 255 / 25%);
        box-shadow:
          0 4px 16px rgb(0 0 0 / 12%),
          0 12px 32px rgb(0 0 0 / 8%);
        backdrop-filter: blur(20px);
        -webkit-backdrop-filter: blur(20px);
        max-width: 300px;
      }

      .ambient-preview {
        position: relative;
        height: 300px;
        border-radius: 16px;
        background: var(--yn-background);
        overflow: hidden;
        display: flex;
        align-items: center;
        justify-content: center;
      }
      .orb {
        position: absolute;
        border-radius: 50%;
        filter: blur(60px);
      }
      .orb-1 {
        width: 250px;
        height: 250px;
        top: 10%;
        left: 10%;
        background: var(--yn-ambient-orb-1);
      }
      .orb-2 {
        width: 200px;
        height: 200px;
        top: 50%;
        right: 15%;
        background: var(--yn-ambient-orb-2);
      }
      .orb-3 {
        width: 180px;
        height: 180px;
        bottom: 5%;
        left: 40%;
        background: var(--yn-ambient-orb-3);
      }
      .glass-on-ambient {
        position: relative;
        padding: 2rem 3rem;
        border-radius: 16px;
        background: rgb(255 255 255 / 12%);
        border: 1px solid rgb(255 255 255 / 25%);
        backdrop-filter: blur(16px);
        -webkit-backdrop-filter: blur(16px);
        box-shadow:
          0 2px 8px rgb(0 0 0 / 8%),
          0 8px 24px rgb(0 0 0 / 6%);
      }
      .glass-on-ambient p {
        margin: 0;
        font-weight: 500;
      }
    `,
  ],
})
class GlassDocsComponent {
  levels = [
    { level: 0, name: 'Base' },
    { level: 1, name: 'Level 1' },
    { level: 2, name: 'Level 2' },
    { level: 3, name: 'Level 3' },
  ];
}

const meta: Meta<GlassDocsComponent> = {
  title: 'Foundations/Glass & Elevation',
  component: GlassDocsComponent,
};

export default meta;
type Story = StoryObj<GlassDocsComponent>;

export const GlassSurfaces: Story = {};

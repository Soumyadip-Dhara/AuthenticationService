import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AuthService, UserClaims } from '../../services/auth.service';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="dashboard">
      <header class="header">
        <div class="header-left">
          <div class="logo-mini">
            <svg viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
              <path fill="currentColor" d="M12 1L3 5v6c0 5.55 3.84 10.74 9 12 5.16-1.26 9-6.45 9-12V5l-9-4zm0 10.99h7c-.53 4.12-3.28 7.79-7 8.94V12H5V6.3l7-3.11v8.8z"/>
            </svg>
          </div>
          <h1>Demo Dashboard</h1>
        </div>
        <button class="logout-btn" (click)="onLogout()">
          <svg viewBox="0 0 24 24" width="18" height="18">
            <path fill="currentColor" d="M17 7l-1.41 1.41L18.17 11H8v2h10.17l-2.58 2.58L17 17l5-5zM4 5h8V3H4c-1.1 0-2 .9-2 2v14c0 1.1.9 2 2 2h8v-2H4V5z"/>
          </svg>
          Sign out
        </button>
      </header>

      <main class="content">
        <!-- User Info Card -->
        <div class="card user-card" *ngIf="authService.user() as user">
          <div class="card-header">
            <h2>👤 Authenticated User</h2>
            <span class="badge badge-success">Active Session</span>
          </div>
          <div class="claims-grid">
            <div class="claim" *ngFor="let claim of getClaimsList(user)">
              <span class="claim-key">{{ claim.key }}</span>
              <span class="claim-value">{{ claim.value }}</span>
            </div>
          </div>
        </div>

        <!-- Security Status Card -->
        <div class="card security-card">
          <div class="card-header">
            <h2>🛡️ Security Status</h2>
          </div>
          <div class="checks">
            <div class="check">
              <span class="check-icon pass">✓</span>
              <div>
                <strong>No tokens in browser storage</strong>
                <p>localStorage and sessionStorage are clean</p>
              </div>
            </div>
            <div class="check">
              <span class="check-icon pass">✓</span>
              <div>
                <strong>Session via opaque cookie only</strong>
                <p>.demo.session cookie — no JWT visible to client</p>
              </div>
            </div>
            <div class="check">
              <span class="check-icon pass">✓</span>
              <div>
                <strong>PKCE authorization code flow</strong>
                <p>Code challenge/verifier used for token exchange</p>
              </div>
            </div>
            <div class="check">
              <span class="check-icon pass">✓</span>
              <div>
                <strong>API access via BFF proxy</strong>
                <p>Access token injected server-side by BFF</p>
              </div>
            </div>
          </div>
        </div>

        <!-- API Test Card -->
        <div class="card api-card">
          <div class="card-header">
            <h2>🔌 API Test</h2>
            <button class="action-btn" (click)="callApi()" [disabled]="apiLoading()">
              {{ apiLoading() ? 'Calling...' : 'Call /api/resource' }}
            </button>
          </div>
          <div *ngIf="apiResponse()" class="api-response">
            <pre>{{ apiResponse() | json }}</pre>
          </div>
          <div *ngIf="apiError()" class="api-error">
            {{ apiError() }}
          </div>
        </div>
      </main>
    </div>
  `,
  styles: [`
    .dashboard {
      min-height: 100vh;
      background: linear-gradient(135deg, #0a0a1a 0%, #1a1a3e 50%, #0f0f2d 100%);
      color: #e0e0e0;
    }

    .header {
      display: flex;
      align-items: center;
      justify-content: space-between;
      padding: 1rem 2rem;
      background: rgba(255, 255, 255, 0.03);
      border-bottom: 1px solid rgba(255, 255, 255, 0.06);
      backdrop-filter: blur(10px);
    }

    .header-left {
      display: flex;
      align-items: center;
      gap: 0.75rem;
    }

    .logo-mini {
      width: 36px;
      height: 36px;
      background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
      border-radius: 10px;
      display: flex;
      align-items: center;
      justify-content: center;
    }

    .logo-mini svg {
      width: 20px;
      height: 20px;
      color: white;
    }

    .header h1 {
      font-size: 1.125rem;
      font-weight: 600;
      color: white;
    }

    .logout-btn {
      display: flex;
      align-items: center;
      gap: 0.375rem;
      padding: 0.5rem 1rem;
      background: rgba(255, 255, 255, 0.06);
      border: 1px solid rgba(255, 255, 255, 0.1);
      border-radius: 10px;
      color: rgba(255, 255, 255, 0.7);
      font-size: 0.8125rem;
      font-family: inherit;
      cursor: pointer;
      transition: all 0.2s;
    }

    .logout-btn:hover {
      background: rgba(239, 68, 68, 0.1);
      border-color: rgba(239, 68, 68, 0.3);
      color: #fca5a5;
    }

    .content {
      max-width: 800px;
      margin: 2rem auto;
      padding: 0 1.5rem;
      display: flex;
      flex-direction: column;
      gap: 1.5rem;
      animation: fadeIn 0.5s ease;
    }

    @keyframes fadeIn {
      from { opacity: 0; transform: translateY(10px); }
      to { opacity: 1; transform: translateY(0); }
    }

    .card {
      background: rgba(255, 255, 255, 0.04);
      border: 1px solid rgba(255, 255, 255, 0.08);
      border-radius: 16px;
      padding: 1.5rem;
    }

    .card-header {
      display: flex;
      align-items: center;
      justify-content: space-between;
      margin-bottom: 1.25rem;
    }

    .card-header h2 {
      font-size: 1rem;
      font-weight: 600;
      color: white;
    }

    .badge {
      font-size: 0.6875rem;
      font-weight: 600;
      padding: 0.25rem 0.625rem;
      border-radius: 100px;
      text-transform: uppercase;
      letter-spacing: 0.05em;
    }

    .badge-success {
      background: rgba(34, 197, 94, 0.15);
      color: #4ade80;
      border: 1px solid rgba(34, 197, 94, 0.2);
    }

    .claims-grid {
      display: grid;
      gap: 0.5rem;
    }

    .claim {
      display: flex;
      gap: 1rem;
      padding: 0.625rem 0.875rem;
      background: rgba(255, 255, 255, 0.02);
      border-radius: 8px;
      font-size: 0.8125rem;
    }

    .claim-key {
      color: rgba(255, 255, 255, 0.5);
      min-width: 80px;
      font-weight: 500;
    }

    .claim-value {
      color: #e0e0e0;
      word-break: break-all;
      font-family: 'SF Mono', 'Fira Code', monospace;
    }

    .checks {
      display: flex;
      flex-direction: column;
      gap: 0.75rem;
    }

    .check {
      display: flex;
      gap: 0.75rem;
      align-items: flex-start;
    }

    .check-icon {
      flex-shrink: 0;
      width: 24px;
      height: 24px;
      border-radius: 50%;
      display: flex;
      align-items: center;
      justify-content: center;
      font-size: 0.75rem;
      font-weight: 700;
    }

    .check-icon.pass {
      background: rgba(34, 197, 94, 0.15);
      color: #4ade80;
    }

    .check strong {
      font-size: 0.8125rem;
      color: white;
      display: block;
      margin-bottom: 0.125rem;
    }

    .check p {
      font-size: 0.75rem;
      color: rgba(255, 255, 255, 0.4);
    }

    .action-btn {
      padding: 0.4375rem 0.875rem;
      background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
      border: none;
      border-radius: 8px;
      color: white;
      font-size: 0.8125rem;
      font-weight: 500;
      font-family: inherit;
      cursor: pointer;
      transition: all 0.2s;
    }

    .action-btn:hover:not(:disabled) {
      transform: translateY(-1px);
      box-shadow: 0 4px 12px rgba(102, 126, 234, 0.3);
    }

    .action-btn:disabled {
      opacity: 0.6;
      cursor: not-allowed;
    }

    .api-response {
      background: rgba(0, 0, 0, 0.2);
      border-radius: 8px;
      padding: 1rem;
      overflow-x: auto;
    }

    .api-response pre {
      font-size: 0.75rem;
      color: #a5b4fc;
      font-family: 'SF Mono', 'Fira Code', monospace;
      white-space: pre-wrap;
      margin: 0;
    }

    .api-error {
      background: rgba(239, 68, 68, 0.1);
      border: 1px solid rgba(239, 68, 68, 0.2);
      border-radius: 8px;
      padding: 0.75rem;
      font-size: 0.8125rem;
      color: #fca5a5;
    }
  `]
})
export class DashboardComponent implements OnInit {
  apiResponse = signal<any>(null);
  apiError = signal<string | null>(null);
  apiLoading = signal(false);

  constructor(public authService: AuthService) {}

  ngOnInit(): void {
    // Auth check already happened via the guard
  }

  getClaimsList(user: UserClaims): { key: string; value: string }[] {
    return Object.entries(user)
      .filter(([_, v]) => typeof v === 'string' || typeof v === 'number')
      .map(([key, value]) => ({ key, value: String(value) }));
  }

  callApi(): void {
    this.apiLoading.set(true);
    this.apiError.set(null);
    this.apiResponse.set(null);

    this.authService.getResource().subscribe({
      next: (data) => {
        this.apiResponse.set(data);
        this.apiLoading.set(false);
      },
      error: (err) => {
        this.apiError.set(`API call failed: ${err.status} ${err.statusText}`);
        this.apiLoading.set(false);
      }
    });
  }

  onLogout(): void {
    this.authService.logout();
  }
}

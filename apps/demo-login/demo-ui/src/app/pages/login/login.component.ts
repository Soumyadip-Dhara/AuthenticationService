import { Component } from '@angular/core';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  template: `
    <div class="login-page">
      <div class="login-card">
        <div class="logo">
          <div class="logo-icon">
            <svg viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
              <path fill="currentColor" d="M12 1L3 5v6c0 5.55 3.84 10.74 9 12 5.16-1.26 9-6.45 9-12V5l-9-4zm0 10.99h7c-.53 4.12-3.28 7.79-7 8.94V12H5V6.3l7-3.11v8.8z"/>
            </svg>
          </div>
          <h1>Demo Application</h1>
          <p>Powered by Auth Service with BFF Pattern</p>
        </div>

        <div class="features">
          <div class="feature">
            <span class="feature-icon">🔒</span>
            <div>
              <strong>No Tokens in Browser</strong>
              <p>OAuth tokens are stored server-side. Only an opaque session cookie is used.</p>
            </div>
          </div>
          <div class="feature">
            <span class="feature-icon">🔗</span>
            <div>
              <strong>SSO Ready</strong>
              <p>Sign in once, access all connected applications seamlessly.</p>
            </div>
          </div>
          <div class="feature">
            <span class="feature-icon">🛡️</span>
            <div>
              <strong>PKCE Protected</strong>
              <p>Authorization code flow with PKCE for maximum security.</p>
            </div>
          </div>
        </div>

        <button class="sign-in-btn" (click)="onLogin()">
          <svg viewBox="0 0 24 24" width="20" height="20" xmlns="http://www.w3.org/2000/svg">
            <path fill="currentColor" d="M11 7L9.6 8.4l2.6 2.6H2v2h10.2l-2.6 2.6L11 17l5-5-5-5zm9 12h-8v2h8c1.1 0 2-.9 2-2V5c0-1.1-.9-2-2-2h-8v2h8v14z"/>
          </svg>
          Sign in with Auth Service
        </button>

        <div class="footer">
          <p>OpenID Connect + BFF Architecture</p>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .login-page {
      min-height: 100vh;
      display: flex;
      align-items: center;
      justify-content: center;
      background: linear-gradient(135deg, #0a0a1a 0%, #1a1a3e 50%, #0f0f2d 100%);
      padding: 1rem;
    }

    .login-card {
      width: 100%;
      max-width: 480px;
      background: rgba(255, 255, 255, 0.04);
      backdrop-filter: blur(20px);
      border: 1px solid rgba(255, 255, 255, 0.08);
      border-radius: 24px;
      padding: 2.5rem;
      animation: slideUp 0.6s cubic-bezier(0.16, 1, 0.3, 1);
    }

    @keyframes slideUp {
      from { opacity: 0; transform: translateY(30px); }
      to { opacity: 1; transform: translateY(0); }
    }

    .logo {
      text-align: center;
      margin-bottom: 2rem;
    }

    .logo-icon {
      width: 64px;
      height: 64px;
      background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
      border-radius: 18px;
      display: inline-flex;
      align-items: center;
      justify-content: center;
      margin-bottom: 1rem;
      box-shadow: 0 4px 20px rgba(102, 126, 234, 0.3);
    }

    .logo-icon svg {
      width: 32px;
      height: 32px;
      color: white;
    }

    .logo h1 {
      font-size: 1.75rem;
      font-weight: 700;
      color: white;
      letter-spacing: -0.02em;
    }

    .logo p {
      font-size: 0.875rem;
      color: rgba(255, 255, 255, 0.5);
      margin-top: 0.25rem;
    }

    .features {
      display: flex;
      flex-direction: column;
      gap: 1rem;
      margin-bottom: 2rem;
    }

    .feature {
      display: flex;
      gap: 0.75rem;
      padding: 0.875rem;
      background: rgba(255, 255, 255, 0.03);
      border-radius: 12px;
      border: 1px solid rgba(255, 255, 255, 0.05);
    }

    .feature-icon {
      font-size: 1.25rem;
      flex-shrink: 0;
    }

    .feature strong {
      display: block;
      font-size: 0.875rem;
      color: white;
      margin-bottom: 0.125rem;
    }

    .feature p {
      font-size: 0.75rem;
      color: rgba(255, 255, 255, 0.5);
      line-height: 1.4;
    }

    .sign-in-btn {
      width: 100%;
      padding: 0.9375rem;
      background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
      border: none;
      border-radius: 14px;
      color: white;
      font-size: 1rem;
      font-weight: 600;
      font-family: inherit;
      cursor: pointer;
      display: flex;
      align-items: center;
      justify-content: center;
      gap: 0.5rem;
      transition: all 0.2s ease;
    }

    .sign-in-btn:hover {
      transform: translateY(-2px);
      box-shadow: 0 6px 24px rgba(102, 126, 234, 0.4);
    }

    .sign-in-btn:active {
      transform: translateY(0);
    }

    .footer {
      text-align: center;
      margin-top: 1.5rem;
      font-size: 0.75rem;
      color: rgba(255, 255, 255, 0.3);
    }
  `]
})
export class LoginComponent {
  constructor(private authService: AuthService) {}

  onLogin(): void {
    this.authService.login();
  }
}

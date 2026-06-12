import { Injectable, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { catchError, of, tap } from 'rxjs';

export interface UserClaims {
  sub: string;
  name: string;
  email: string;
  sid?: string;
  [key: string]: unknown;
}

/**
 * Auth service that communicates with the BFF.
 * 
 * Key principle: No tokens are stored in the browser.
 * All auth state is managed by the BFF via an opaque session cookie.
 */
@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private _user = signal<UserClaims | null>(null);
  private _loading = signal(true);
  private _checked = signal(false);

  /** Current authenticated user, or null */
  readonly user = this._user.asReadonly();

  /** Whether auth check is in progress */
  readonly loading = this._loading.asReadonly();

  /** Whether user is authenticated */
  readonly isAuthenticated = computed(() => this._user() !== null);

  constructor(private http: HttpClient) {}

  /**
   * Check authentication status by calling GET /bff/user.
   * If the BFF returns 200, the user is authenticated.
   * If 401, the user is not authenticated.
   */
  checkAuth() {
    this._loading.set(true);
    return this.http.get<UserClaims>('/bff/user', { withCredentials: true }).pipe(
      tap(user => {
        this._user.set(user);
        this._loading.set(false);
        this._checked.set(true);
      }),
      catchError(() => {
        this._user.set(null);
        this._loading.set(false);
        this._checked.set(true);
        return of(null);
      })
    );
  }

  /**
   * Trigger login by redirecting to the BFF's /bff/login endpoint.
   * This starts the OIDC authorization code flow with PKCE.
   * Full page redirect — not an AJAX call.
   */
  login(): void {
    window.location.href = '/bff/login';
  }

  /**
   * Trigger logout by redirecting to the BFF's /bff/logout endpoint.
   * This clears the app session and redirects to IdP logout.
   */
  logout(): void {
    window.location.href = '/bff/logout';
  }

  /**
   * Trigger app-only logout by redirecting to the BFF's /bff/applogout endpoint.
   * This clears the client app session but keeps the IdP session active.
   */
  appLogout(): void {
    window.location.href = '/bff/applogout';
  }

  /**
   * Call a protected API resource through the BFF proxy.
   * The BFF injects the access token server-side.
   */
  getResource() {
    return this.http.get<any>('/api/resource', { withCredentials: true });
  }

  /**
   * Get the authenticated user's claims as seen by the API.
   */
  getApiMe() {
    return this.http.get<any>('/api/me', { withCredentials: true });
  }
}

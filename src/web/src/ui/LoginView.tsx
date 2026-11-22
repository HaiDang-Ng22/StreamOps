import { FormEvent, useState } from 'react';
import { signIn } from '../api/client';

type Props = { onSignedIn: (session: { displayName: string; roles: string[] }) => void };

export default function LoginView({ onSignedIn }: Props) {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');

  async function submit(event: FormEvent) {
    event.preventDefault();
    setBusy(true);
    setError('');
    try {
      const result = await signIn(email, password);
      onSignedIn({ displayName: result.displayName, roles: result.roles });
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : 'Không thể đăng nhập lúc này.');
    } finally {
      setBusy(false);
    }
  }

  return <main className="login-page">
    <section className="login-card">
      <div className="login-brand"><span className="brand-mark">S</span><span>stream<span className="brand-light">ops</span></span></div>
      <div className="eyebrow">CỔNG ĐIỀU HÀNH AGENCY</div>
      <h1>Chào mừng trở lại</h1>
      <p>Đăng nhập bằng tài khoản do công ty cấp.</p>
      <form onSubmit={submit} className="login-form">
        <label>Email công ty<input type="email" autoComplete="username" required maxLength={254} value={email} onChange={event => setEmail(event.target.value)} /></label>
        <label>Mật khẩu<input type="password" autoComplete="current-password" required maxLength={128} value={password} onChange={event => setPassword(event.target.value)} /></label>
        {error && <div className="form-error" role="alert">{error}</div>}
        <button className="primary-button login-submit" disabled={busy}>{busy ? 'Đang đăng nhập…' : 'Đăng nhập'}</button>
      </form>
      <div className="login-note"><span className="secure-dot"/> Không có đăng ký tự do. Cần quyền truy cập? Liên hệ quản trị viên agency.</div>
    </section>
    <div className="login-decoration"><div className="orb orb-one"/><div className="orb orb-two"/><span className="decoration-mark">S</span></div>
  </main>;
}

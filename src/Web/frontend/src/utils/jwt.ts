export interface JwtPayload {
  sub?: string
  name?: string
  email?: string
  role?: string
  [key: string]: unknown
}

/**
 * Decodes the payload of a signed (but not encrypted) JWT without verifying it.
 * OpenIddict issues readable access tokens (DisableAccessTokenEncryption), so the
 * `role` claim can be read client-side for UI gating (Spec 07 Req 4).
 */
export function decodeJwt(token: string): JwtPayload | null {
  try {
    const segment = token.split('.')[1]
    if (!segment) return null
    let base64 = segment.replace(/-/g, '+').replace(/_/g, '/')
    while (base64.length % 4 !== 0) base64 += '='
    const json = decodeURIComponent(
      window
        .atob(base64)
        .split('')
        .map((c) => '%' + c.charCodeAt(0).toString(16).padStart(2, '0'))
        .join(''),
    )
    return JSON.parse(json) as JwtPayload
  } catch {
    return null
  }
}

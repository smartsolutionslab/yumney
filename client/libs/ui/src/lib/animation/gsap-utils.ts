import gsap from 'gsap';

/**
 * Spring animation — press effect for buttons/cards
 */
export function springPress(element: Element, scale = 0.96): gsap.core.Tween {
  return gsap.to(element, {
    scale,
    duration: 0.1,
    ease: 'power2.out',
    yoyo: true,
    repeat: 1,
  });
}

/**
 * Staggered fade in — for lists of items
 */
export function staggerFadeIn(
  elements: Element[] | NodeListOf<Element>,
  options: { stagger?: number; duration?: number; y?: number } = {},
): gsap.core.Tween {
  const { stagger = 0.05, duration = 0.4, y = 16 } = options;
  return gsap.fromTo(
    elements,
    { opacity: 0, y },
    { opacity: 1, y: 0, duration, stagger, ease: 'power2.out' },
  );
}

/**
 * Check if user prefers reduced motion
 */
export function prefersReducedMotion(): boolean {
  if (typeof window === 'undefined' || typeof window.matchMedia !== 'function') {
    return false;
  }
  return window.matchMedia('(prefers-reduced-motion: reduce)').matches;
}

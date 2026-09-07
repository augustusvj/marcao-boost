'use client';

import { useEffect, useRef } from 'react';

export function MatrixBackground() {
  const canvasRef = useRef<HTMLCanvasElement>(null);
  useEffect(() => {
    const canvas = canvasRef.current;
    const context = canvas?.getContext('2d');
    if (!canvas || !context) return;
    let frame = 0;
    let columns: number[] = [];
    const glyphs = '01MARCÃOBOOST<>/{}[]';
    const fontSize = 18;
    const resize = () => {
      const ratio = Math.min(window.devicePixelRatio || 1, 2);
      canvas.width = window.innerWidth * ratio;
      canvas.height = window.innerHeight * ratio;
      canvas.style.width = `${window.innerWidth}px`;
      canvas.style.height = `${window.innerHeight}px`;
      context.setTransform(ratio, 0, 0, ratio, 0, 0);
      columns = Array(Math.ceil(window.innerWidth / fontSize))
        .fill(0)
        .map(() => Math.random() * -60);
    };
    const draw = () => {
      context.fillStyle = 'rgba(2, 7, 4, 0.07)';
      context.fillRect(0, 0, window.innerWidth, window.innerHeight);
      context.font = `500 ${fontSize}px monospace`;
      columns.forEach((position, index) => {
        const bright = Math.random() > 0.975;
        context.fillStyle = bright ? '#d7ffe1' : '#19f45a';
        context.shadowBlur = bright ? 10 : 3;
        context.shadowColor = '#14f051';
        context.fillText(
          glyphs[Math.floor(Math.random() * glyphs.length)],
          index * fontSize,
          position * fontSize,
        );
        columns[index] =
          position * fontSize > window.innerHeight && Math.random() > 0.97
            ? 0
            : position + 0.48;
      });
      context.shadowBlur = 0;
      frame = requestAnimationFrame(draw);
    };
    resize();
    window.addEventListener('resize', resize);
    frame = requestAnimationFrame(draw);
    return () => {
      window.removeEventListener('resize', resize);
      cancelAnimationFrame(frame);
    };
  }, []);
  return (
    <canvas ref={canvasRef} className="matrix-canvas" aria-hidden="true" />
  );
}

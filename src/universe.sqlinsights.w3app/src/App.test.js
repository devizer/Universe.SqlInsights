import React from 'react';
import ReactDOM from 'react-dom';
import { MemoryRouter } from 'react-router-dom';

import { render, screen } from '@testing-library/react';
import App from './App';

test('renders app', () => {
  render(<App />);
});

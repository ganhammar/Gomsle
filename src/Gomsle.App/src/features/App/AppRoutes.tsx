import { Route, Routes } from 'react-router-dom';
import Landing from '../Landing';
import { Register } from '../User/Register';

export function AppRoutes() {
  return (
    <Routes>
      <Route path="/" element={<Landing />} />
      <Route path="/register" element={<Register />} />
    </Routes>
  );
}

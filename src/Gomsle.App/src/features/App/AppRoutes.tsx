import { Route, Routes } from "react-router-dom";
import Landing from "../Landing";

export function AppRoutes() {
  return (
    <Routes>
      <Route path="/" element={<Landing />} />
    </Routes>
  );
}

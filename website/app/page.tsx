import Nav from "@/components/Nav";
import Hero from "@/components/Hero";
import StatsBar from "@/components/StatsBar";
import Products from "@/components/Products";
import HowItWorks from "@/components/HowItWorks";
import CtaBanner from "@/components/CtaBanner";
import Footer from "@/components/Footer";

export default function Home() {
  return (
    <>
      <Nav />
      <main>
        <Hero />
        <StatsBar />
        <Products />
        <HowItWorks />
        <CtaBanner />
      </main>
      <Footer />
    </>
  );
}
